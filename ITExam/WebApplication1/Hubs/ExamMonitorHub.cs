using ITExam.Models;
using ITExam.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ITExam.Hubs
{
    public class ConnectionMapValue
    {
        public string ExamKey { get; set; } 
        public string StudentId { get; set; }
    }

    public class ExamMonitorHub : Hub
    {
        private readonly ITExamDbContext _db;
        private readonly RedisService _redis;
        private readonly IMemoryCache _cache;

        public ExamMonitorHub(RedisService redis, ITExamDbContext db, IMemoryCache cache)
        {
            _redis = redis;
            _db = db;
            _cache = cache;
        }

        // === Helpers cho Cache ===
        // Key cho danh sách thí sinh đang tham gia
        private static string GetParticipantsCacheKey(string examKey) => $"participants:{examKey}";
        // Key cho cấu hình anti-cheat
        private static string GetAntiCheatCacheKey(string examId, string classId) => $"anticheat:{examId}:{classId}";
        // Key cho danh sách điểm
        private static string GetScoresCacheKey(string examKey) => $"scores:{examKey}";
        // Thời gian hết hạn trượt
        private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(3);
        // Helper cho khóa map ngược
        private static string GetConnectionMapKey(string connectionId) => $"conn_map:{connectionId}";

        // Helper để lấy (hoặc tạo) danh sách thí sinh
        private ConcurrentDictionary<string, StudentStatus> GetParticipants(string examKey)
        {
            return _cache.GetOrCreate(GetParticipantsCacheKey(examKey), entry =>
            {
                entry.SetSlidingExpiration(CacheSlidingExpiration);
                return new ConcurrentDictionary<string, StudentStatus>();
            });
        }

        // Helper để lấy (hoặc tạo) danh sách điểm
        private ConcurrentDictionary<string, double> GetScores(string examKey)
        {
            return _cache.GetOrCreate(GetScoresCacheKey(examKey), entry =>
            {
                entry.SetSlidingExpiration(CacheSlidingExpiration);
                return new ConcurrentDictionary<string, double>();
            });
        }

        // ====== Models & helpers ======
        public class StudentStatus
        {
            public string? ExamId { get; set; } 
            public string? ClassId { get; set; }
            public string? StudentId { get; set; }
            public string? StudentName { get; set; }
            public string? ConnectionId { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsConnected { get; set; } = true;
            public int TabLeaveCount { get; set; } = 0;
            public DateTime JoinedAt { get; set; }
            public DateTime? LastActivityAt { get; set; }
            public string? IP { get; set; }
            public string? UserAgent { get; set; }
        }

        private static string ExamKey(string examId, string classId) => $"{examId}:{classId}";
        private static string GroupName(string examId, string classId) => $"exam:{examId}:class:{classId}";
        private static string LogKey(string examId, string classId, string studentId) => $"ITExam_logs:{examId}:{classId}:{studentId}";
        private static string TabCountKey(string examId, string classId, string studentId) => $"ITExam_tabcount:{examId}:{classId}:{studentId}";


        private void UpdateStudentActivity(string examId, string classId, string studentId)
        {
            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status))
            {
                status.IsActive = true;
                status.LastActivityAt = DateTime.UtcNow;
            }
        }

        private static string ExtractPrefix(string message)
        {
            var m = Regex.Match(message, @"^\[(.*?)\]");
            return m.Success ? m.Value : "[INFO]";
        }

        // ====== Hub methods ======
        /// Sinh viên tham gia bài thi (khởi tạo, phục hồi TabLeaveCount)
        public async Task JoinExam(string examId, string classId, string studentId, string studentName)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(studentName))
                throw new HubException("Missing required parameters");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(examId, classId));

            var keyp = ExamKey(examId, classId);
            //var participants = _examParticipants.GetOrAdd(keyp, _ => new ConcurrentDictionary<string, StudentStatus>());
            var participants = GetParticipants(keyp);
            // Phục hồi TabLeaveCount từ Redis (nếu có)
            int currentTabCount = 0;
            var tabStr = await _redis.GetStringAsync(TabCountKey(examId, classId, studentId));
            if (!string.IsNullOrEmpty(tabStr) && int.TryParse(tabStr, out var saved)) currentTabCount = saved;

            var status = new StudentStatus
            {
                ExamId = examId,
                ClassId = classId,
                StudentId = studentId,
                StudentName = studentName,
                ConnectionId = Context.ConnectionId,
                IsActive = true,
                IsConnected = true,
                TabLeaveCount = currentTabCount,
                JoinedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };
            participants[studentId] = status;
            // Cập nhật map ngược ConnectionId -> (ExamKey, StudentId)
            var mapKey = GetConnectionMapKey(Context.ConnectionId);
            var mapValue = new ConnectionMapValue { ExamKey = keyp, StudentId = studentId };

            _cache.Set(mapKey, mapValue, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSlidingExpiration // Dùng chung thời gian hết hạn
            });

            // Thông báo cho giám sát
            await Clients.Group(GroupName(examId, classId)).SendAsync("StudentJoined", new
            {
                ExamId = examId,
                StudentId = studentId,
                StudentName = studentName,
                JoinedAt = status.JoinedAt,
                IsActive = status.IsActive,
                TabLeaveCount = status.TabLeaveCount
            });

            await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                $"[JOIN] [{DateTime.Now:HH:mm:ss}] {studentName} đã tham gia bài thi");

            // Gửi lại AntiCheat config cho client mới vào nếu có
            var antiCheatKey = GetAntiCheatCacheKey(examId, classId);
            if (_cache.TryGetValue(antiCheatKey, out var config))
                await Clients.Caller.SendAsync("ReceiveAntiCheatConfig", config);
        }

        /// Nhận cấu trúc đề thi từ client
        public async Task SendFullExamStructure(string examId, string classId, string studentId, string studentName, object examStructure)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId) || examStructure is null)
                throw new HubException("Thiếu tham số bắt buộc");

            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status) &&
                status.IsConnected)
            {
                {
                    UpdateStudentActivity(examId, classId, studentId);

                    var redisKey = $"ITExam_examStructure:{examId}:{classId}:{studentId}";
                    await _redis.SetStringAsync(redisKey, JsonSerializer.Serialize(examStructure));

                    await Clients.Group(GroupName(examId, classId)).SendAsync("StudentSentExamStructure", new
                    {
                        examId,
                        studentId,
                        studentName,
                        timestamp = DateTime.UtcNow,
                        structure = examStructure
                    });
                }
            }
        }

        /// Sinh viên chọn đáp án
        public async Task SelectAnswer(string examId, string classId, string studentId, string studentName,
            string questionId, string questionContent, string answerId, string answerContent)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                throw new HubException("Thiếu tham số");

            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status) &&
                status.IsConnected)
            {
                UpdateStudentActivity(examId, classId, studentId);

                await Clients.Group(GroupName(examId, classId)).SendAsync("StudentSelectedAnswer", new
                {
                    ExamId = examId,
                    StudentId = studentId,
                    StudentName = studentName,
                    QuestionId = questionId,
                    QuestionContent = questionContent,
                    AnswerId = answerId,
                    AnswerContent = answerContent,
                    Timestamp = DateTime.UtcNow
                });

                await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                    $"[ANSWER] [{DateTime.Now:HH:mm:ss}] {studentName} - Câu hỏi \"{questionContent}\" --> \"{answerContent}\"");
            }
        }

        /// Thay đổi trạng thái tab
        public async Task DetectTabChange(string examId, string classId, string studentId, string studentName, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                throw new HubException("Thiếu tham số");

            var keyp = ExamKey(examId, classId);
            var participants = GetParticipants(keyp);
            if (participants.TryGetValue(studentId, out var status) && status.IsConnected)
            {
                status.IsActive = isActive;
                status.LastActivityAt = DateTime.UtcNow;

                if (!isActive)
                {
                    status.TabLeaveCount += 1;
                    await _redis.SetStringAsync(TabCountKey(examId, classId, studentId), status.TabLeaveCount.ToString());
                }

                await Clients.Group(GroupName(examId, classId)).SendAsync("StudentTabChanged", new
                {
                    ExamId = examId,
                    StudentId = studentId,
                    StudentName = studentName,
                    IsActive = isActive,
                    TabLeaveCount = status.TabLeaveCount,
                    Timestamp = DateTime.UtcNow
                });

                var msg = !isActive
                    ? $"[TABOUT] [{DateTime.Now:HH:mm:ss}] {studentName} chuyển khỏi tab làm bài (lần thứ {status.TabLeaveCount})"
                    : $"[TABIN] [{DateTime.Now:HH:mm:ss}] {studentName} quay lại tab làm bài";
                await _redis.AddLogAsync(LogKey(examId, classId, studentId), msg);
            }
        }

        /// Sinh viên nộp bài
        public async Task SubmitExam(string examId, string classId, string studentId, string studentName)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                throw new HubException("Missing required parameters");

            var keyp = ExamKey(examId, classId);
            int tabLeaveCount = 0;

            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status))
            {
                status.IsActive = false;
                status.IsConnected = false;
                status.LastActivityAt = DateTime.UtcNow;
                tabLeaveCount = status.TabLeaveCount;
            }

            _cache.Remove(GetConnectionMapKey(Context.ConnectionId));

            await Clients.Group(GroupName(examId, classId)).SendAsync("StudentSubmittedExam", new
            {
                ExamId = examId,
                StudentId = studentId,
                StudentName = studentName,
                TabLeaveCount = tabLeaveCount,
                SubmittedAt = DateTime.UtcNow
            });

            await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                $"[SUBMIT] [{DateTime.Now:HH:mm:ss}] {studentName} đã nộp bài thi. Số lần chuyển tab {tabLeaveCount}");
        }

        /// Sinh viên rời phòng
        public async Task LeaveExam(string examId, string classId, string studentId, string studentName)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                throw new HubException("Missing required parameters");

            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status))
            {
                status.IsActive = false;
                status.IsConnected = false;
                status.LastActivityAt = DateTime.UtcNow;
            }

            _cache.Remove(GetConnectionMapKey(Context.ConnectionId));

            await Clients.Group(GroupName(examId, classId)).SendAsync("StudentLeftExam", new
            {
                ExamId = examId,
                StudentId = studentId,
                StudentName = studentName,
                LeftAt = DateTime.UtcNow
            });

            await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                $"[LEFT] [{DateTime.Now:HH:mm:ss}] {studentName} đã rời khỏi phòng thi");
        }

        /// Giám thị tham gia giám sát (bù trạng thái từ runtime + DB)
        public async Task JoinMonitor(string examId, string classId)
        {
            if (string.IsNullOrWhiteSpace(examId))
                throw new HubException("Missing examId parameter");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(examId, classId));

            // 1) Phát lại trạng thái runtime (đang connected/disconnected)
            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants))
            {
                foreach (var st in participants.Values)
                {
                    if (st.IsConnected)
                    {
                        await Clients.Caller.SendAsync("StudentJoined", new
                        {
                            ExamId = examId,
                            StudentId = st.StudentId,
                            StudentName = st.StudentName,
                            IsActive = st.IsActive,
                            JoinedAt = st.JoinedAt,
                            IsConnected = st.IsConnected
                        });
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("StudentDisconnected", new
                        {
                            ExamId = examId,
                            StudentId = st.StudentId,
                            StudentName = st.StudentName,
                            DisconnectedAt = st.LastActivityAt
                        });
                    }
                }
            }

            // 2) Bù trạng thái "ĐÃ NỘP" + "ĐIỂM" từ DB (ExamHistory)
            int examIdInt = int.Parse(examId);
            int classIdInt = int.Parse(classId);

            var submitted = await _db.ExamHistories
                .AsNoTracking()
                .Where(h => h.ExamId == examIdInt && h.ClassId == classIdInt && h.SubmitTime != null)
                .Include(h => h.User)
                .Select(h => new
                {
                    h.User.Username,
                    FullName = h.User.FullName ?? "",
                    h.Score,
                    h.Duration,
                    h.SubmitTime
                })
                .ToListAsync();

            foreach (var h in submitted)
            {
                var sid = h.Username;
                var name = h.FullName;

                var tabStr = await _redis.GetStringAsync(TabCountKey(examId, classId, sid));
                int.TryParse(tabStr, out var tabCount);

                // Đánh dấu đã nộp
                await Clients.Caller.SendAsync("StudentSubmittedExam", new
                {
                    ExamId = examId,
                    StudentId = sid,
                    StudentName = name,
                    TabLeaveCount = tabCount,
                    SubmittedAt = h.SubmitTime!.Value.ToUniversalTime()
                });

                // Có điểm -> gửi luôn
                if (h.Score.HasValue)
                {
                    await Clients.Caller.SendAsync("StudentScoreUpdated", new
                    {
                        ExamId = examId,
                        ClassId = classId,
                        StudentId = sid,
                        StudentName = name,
                        Score = h.Score.Value,
                        Duration = h.Duration
                    });
                }
            }

            // 3) (tùy chọn) phát thêm điểm từ cache memory nếu có
            var memKey = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetScoresCacheKey(memKey), out ConcurrentDictionary<string, double> scoreMap))
            {
                foreach (var kv in scoreMap)
                {
                    // (Chỉ gửi nếu DB chưa gửi, hoặc để ghi đè)
                    await Clients.Caller.SendAsync("StudentScoreUpdated", new
                    {
                        ExamId = examId,
                        ClassId = classId,
                        StudentId = kv.Key,
                        StudentName = "", // Tên có thể không quan trọng ở đây
                        Score = kv.Value
                    });
                }
            }
        }

        /// Client báo tiến trình làm bài
        public async Task SendRemainingTime(string examId, string classId, string studentId, int timeLeft, int answeredCount, int totalQuestions)
        {
            var keyp = ExamKey(examId, classId);
            if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                participants.TryGetValue(studentId, out var status) &&
                status.IsConnected)
            {
                await Clients.Group(GroupName(examId, classId)).SendAsync("UpdateStudentProgress", new
                {
                    ExamId = examId,
                    StudentId = studentId,
                    TimeLeft = timeLeft,
                    AnsweredCount = answeredCount,
                    totalQuestions = totalQuestions
                });
            }
        }

        /// Gửi cấu hình AntiCheat xuống tất cả sinh viên
        public async Task SendAntiCheatConfig(string examId, string classId, object config)
        {
            if (string.IsNullOrWhiteSpace(examId))
                throw new HubException("Missing examId parameter");

            _cache.Set(GetAntiCheatCacheKey(examId, classId), config, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSlidingExpiration
            });
            await Clients.Group(GroupName(examId, classId)).SendAsync("ReceiveAntiCheatConfig", config);
        }

        /// Kết thúc giám sát: ghi log vào DB và dọn Redis
        public async Task CompleteMonitoring(string examId, string classId, string instructorName)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(classId))
                throw new HubException("Thiếu mã đề hoặc mã lớp");

            string redisPrefix = $"ITExam_logs:{examId}:{classId}:";
            var keys = await _redis.SearchKeysAsync($"{redisPrefix}*");

            foreach (var key in keys)
            {
                string studentId = key.Split(':').Last();
                var logs = await _redis.GetLogsAsync(key);
                if (logs == null || logs.Count == 0) continue;

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == studentId);
                if (user == null) continue;

                string logContent = string.Join("\n", logs.Take(2000));
                _db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.UserId,
                    ExamId = int.Parse(examId),
                    InstructorName = instructorName,
                    ClassId = int.Parse(classId),
                    LogContent = logContent,
                    LogDate = DateTime.Now
                });

                // Xóa Redis sau khi ghi DB
                await _redis.DeleteKeyAsync(key);
                await _redis.DeleteKeysByPatternAsync($"ITExam_examStructure:{examId}:{classId}:{studentId}");
                await _redis.DeleteKeysByPatternAsync($"ITExam_ip:{studentId}");
                await _redis.DeleteKeysByPatternAsync($"ITExam_ua:{studentId}");
                await _redis.DeleteKeyAsync(TabCountKey(examId, classId, studentId));
            }

            await _db.SaveChangesAsync();
            // DỌN DẸP IMemoryCache
            var examKey = ExamKey(examId, classId);
            _cache.Remove(GetParticipantsCacheKey(examKey));
            _cache.Remove(GetScoresCacheKey(examKey));
            _cache.Remove(GetAntiCheatCacheKey(examId,classId));
        }

        // ====== Connection lifecycle ======
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[Connected] ConnectionId={Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var mapKey = GetConnectionMapKey(Context.ConnectionId);

            if (_cache.TryGetValue(mapKey, out ConnectionMapValue mapValue))
            {
                var keyp = mapValue.ExamKey;
                var studentId = mapValue.StudentId;

                var keyParts = keyp.Split(':');
                var examId = keyParts.Length > 0 ? keyParts[0] : null;
                var classId = keyParts.Length > 1 ? keyParts[1] : null;

                if (!string.IsNullOrEmpty(studentId) && !string.IsNullOrEmpty(examId) && !string.IsNullOrEmpty(classId))
                {
                    string sname = studentId; 

                    if (_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) &&
                        participants.TryGetValue(studentId, out var status))
                    {
                        if (status.IsConnected)
                        {
                            status.IsActive = false;
                            status.IsConnected = false;
                            status.LastActivityAt = DateTime.UtcNow;
                            sname = status.StudentName; // Lấy tên thật

                            await Clients.Group(GroupName(examId, classId)).SendAsync("StudentDisconnected", new
                            {
                                ExamId = examId,
                                StudentId = studentId,
                                StudentName = sname,
                                DisconnectedAt = DateTime.UtcNow
                            });

                            await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                                $"[DISCONNECT] [{DateTime.Now:HH:mm:ss}] {sname} ngắt kết nối (đột ngột)");
                        }
                    }
                }

                _cache.Remove(mapKey);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// Gửi IP/UA & log thay đổi thiết bị/mạng
        public async Task SendClientInfo(string examId, string classId, string studentId, string studentName, string ip, string userAgent)
        {
            var keyp = ExamKey(examId, classId);
            if (!_cache.TryGetValue(GetParticipantsCacheKey(keyp), out ConcurrentDictionary<string, StudentStatus> participants) ||
                !participants.TryGetValue(studentId, out var status))
                return;

            var now = DateTime.Now;
            string? oldIp = await _redis.GetStringAsync($"ITExam_ip:{studentId}");
            string? oldUa = await _redis.GetStringAsync($"ITExam_ua:{studentId}");

            status.IP = ip;
            status.UserAgent = userAgent;

            string? logMessage = null;
            bool isFraud = false;

            if (!string.IsNullOrEmpty(oldIp) && !string.IsNullOrEmpty(oldUa))
            {
                if (oldIp != ip && oldUa != userAgent)
                {
                    logMessage = $"[DEVICE] {studentName} đã thay đổi mạng và trình duyệt từ {oldIp} / {oldUa} --> {ip} / {userAgent} (CẢNH BÁO)";
                    isFraud = true;
                }
                else if (oldIp != ip)
                {
                    logMessage = $"[IP] {studentName} đã thay đổi mạng từ {oldIp} --> {ip}";
                    isFraud = true;
                }
                else if (oldUa != userAgent)
                {
                    logMessage = $"[UA] {studentName} đã thay đổi trình duyệt từ {oldUa} --> {userAgent}";
                    isFraud = true;
                }
            }
            else
            {
                logMessage = $"[INFO] {studentName} IP: {ip}. Trình duyệt: {userAgent}";
            }

            if (!string.IsNullOrEmpty(logMessage))
            {
                string prefix = ExtractPrefix(logMessage);
                string logRedis = $"{prefix} [{now:HH:mm:ss}] {logMessage.Substring(prefix.Length).Trim()}";
                await _redis.AddLogAsync(LogKey(examId, classId, studentId), logRedis);

                await Clients.Group(GroupName(examId, classId)).SendAsync("ReceiveSystemLog", new
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    LogMessage = logMessage,
                    LogType = isFraud ? "fraud" : "info"
                });
            }

            await _redis.SetStringAsync($"ITExam_ip:{studentId}", ip);
            await _redis.SetStringAsync($"ITExam_ua:{studentId}", userAgent);

            await Clients.Group(GroupName(examId, classId)).SendAsync("StudentClientInfoUpdated", new
            {
                ExamId = examId,
                StudentId = studentId,
                studentName = studentName,
                IP = ip,
                UserAgent = userAgent
            });
        }

        /// Ghi nhận resize cửa sổ
        public async Task DetectWindowResize(string examId, string classId, string studentId, string studentName, object resizeInfo)
        {
            if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId) || resizeInfo == null)
                throw new HubException("Missing required parameters or resize info");

            dynamic resizeData = Newtonsoft.Json.JsonConvert.DeserializeObject(resizeInfo.ToString()!);

            bool pinchZoomLikely = resizeData.flags?.pinchZoomLikely ?? false;
            bool keyboardLikely = resizeData.flags?.keyboardLikely ?? false;
            bool urlBarLikely = resizeData.flags?.urlBarLikely ?? false;

            // Bỏ qua sự kiện nếu nhiều khả năng là pinch-zoom / bàn phím ảo / thanh URL ẩn-hiện
            if (pinchZoomLikely || keyboardLikely || urlBarLikely)
                return;

            int initialWidth = resizeData.initialWidth;
            int initialHeight = resizeData.initialHeight;
            int currentWidth = resizeData.currentWidth;
            int currentHeight = resizeData.currentHeight;
            string timestamp = resizeData.timestamp;

            await Clients.Group(GroupName(examId, classId)).SendAsync("WindowResized", new
            {
                ExamId = examId,
                StudentId = studentId,
                StudentName = studentName,
                InitialWidth = initialWidth,
                InitialHeight = initialHeight,
                CurrentWidth = currentWidth,
                CurrentHeight = currentHeight,
                Timestamp = timestamp
            });

            await _redis.AddLogAsync(LogKey(examId, classId, studentId),
                $"[TABOUT] [{DateTime.Now:HH:mm:ss}] {studentName} đã thay đổi kích thước cửa sổ từ {initialWidth}x{initialHeight} thành {currentWidth}x{currentHeight}");
        }
    }
}

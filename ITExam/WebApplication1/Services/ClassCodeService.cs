using System.Security.Cryptography;
using ITExam.Models;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Services
{
    public interface IClassCodeService
    {
        Task<string> GenerateUniqueClassCodeAsync();
        Task<bool> IsCodeExistAsync(string code);
    }
    public class ClassCodeService : IClassCodeService
    {
        private readonly ITExamDbContext _context;
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; 
        private const int CodeLength = 5;
        private const int MaxAttempts = 30; 

        public ClassCodeService(ITExamDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueClassCodeAsync()
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var candidate = GenerateRandomCode();
                if (!await IsCodeExistAsync(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"Không thể sinh mã duy nhất sau {MaxAttempts} lần thử. Hãy tăng MaxAttempts hoặc kiểm tra logic tạo mã.");
        }

        public Task<bool> IsCodeExistAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Task.FromResult(false);

            return _context.Classes.AnyAsync(c => c.ClassCode == code);
        }

        private static string GenerateRandomCode()
        {
            var chars = new char[CodeLength];

            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[4];

            for (int i = 0; i < CodeLength; i++)
            {
                rng.GetBytes(buffer);
                uint value = BitConverter.ToUInt32(buffer, 0);
                chars[i] = Alphabet[(int)(value % (uint)Alphabet.Length)];
            }

            return new string(chars);
        }
    }

}

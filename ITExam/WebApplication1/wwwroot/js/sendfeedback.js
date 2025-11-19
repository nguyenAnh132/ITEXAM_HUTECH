
$("#btnSendFeedback").click(function () {
        const content = $("#feedbackInput").val().trim();

if (content === "") {
    $("#feedbackMessage").html("<div class='text-danger mt-2'>Vui lòng nhập nội dung góp ý.</div>");
return;
        }

$.ajax({
    url: "/feedback/send",
type: "POST",
data: JSON.stringify(content),
contentType: "application/json; charset=utf-8",
success: function (res) {
    $("#feedbackMessage").html("<div class='text-success mt-2'>" + res.message + "</div>");
$("#feedbackInput").val("");
            },
error: function (xhr) {
    let msg = xhr.responseJSON?.message || "Lỗi không xác định!";
$("#feedbackMessage").html("<div class='text-danger mt-2'>" + msg + "</div>");
            }
        });
    });

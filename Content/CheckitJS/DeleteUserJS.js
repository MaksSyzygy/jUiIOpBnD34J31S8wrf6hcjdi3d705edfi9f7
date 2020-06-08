$(function () {
    $("a.delete").click(function () {
        if (!confirm("Удалить пользователя?")) {
            return false;
        }
    });
});
$(function () {
    $("a.delete").click(function () {
        if (!confirm("Удалить закладку?")) {
            return false;
        }
    });
});
$(function () {
    $("button.deleteCat").click(function () {
        if (!confirm("Удалить категорию? Все закладки из этой категории будут удалены")) {
            return false;
        }
    });
});
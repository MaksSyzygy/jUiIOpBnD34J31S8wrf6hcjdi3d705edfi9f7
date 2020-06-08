$(document).on('click', 'button.subscribe', function () {
    if (!confirm('Подписаться на пользователя? Вы будете видеть публичные закладки этого пользователя')) {
        return false;
    }
});
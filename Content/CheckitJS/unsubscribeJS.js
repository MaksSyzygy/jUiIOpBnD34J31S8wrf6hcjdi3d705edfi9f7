$(document).on('click', 'button.unsubscribe', function () {
    if (!confirm('Отписаться от пользователя? Вы более не будете видеть публичные закладки этого пользователя')) {
        return false;
    }
});
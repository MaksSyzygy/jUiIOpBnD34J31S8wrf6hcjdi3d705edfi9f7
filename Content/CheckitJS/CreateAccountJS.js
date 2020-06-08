$('a.createAccount').click(function (event) {
    event.preventDefault();
    $('#myOverlay').fadeIn(297, function () {
        $('#newAccount')
            .css('display', 'block')
            .animate({ opacity: 1 }, 198);
    });
});

$('#Account_Close').click(function () {
    $('#close').animate({ opacity: 0 }, 198, function () {
        $(this).css('display', 'none');
        $('#myOverlay').fadeOut(297);
    });
});
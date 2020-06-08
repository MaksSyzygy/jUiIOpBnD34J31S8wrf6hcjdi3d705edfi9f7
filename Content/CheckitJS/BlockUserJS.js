$('button.blockLink').click(function (event) {
    event.preventDefault();
    $('#myOverlay').fadeIn(297, function () {
        $('#blockUser')
            .css('display', 'block')
            .animate({ opacity: 1 }, 198);
    });
});

$('#blockUser_Close, #myOverlay').click(function () {
    $('#blockUser').animate({ opacity: 0 }, 198, function () {
        $(this).css('display', 'none');
        $('#myOverlay').fadeOut(297);
    });
});
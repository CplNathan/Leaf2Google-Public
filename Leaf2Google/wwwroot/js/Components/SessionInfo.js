$('table').on('click', '#deleteAuth', function () {
    $.ajax({
        url: api.Auth.Delete + "/?authId=" + $(this).attr('data-itemid'),
        type: "POST",
        data: JSON.stringify({ sessionId: $(this).attr('data-itemid') }),
        contentType: "application/json",
        cache: false,
        async: false,
        success: function (data) {
            $('#sessionWrapper').html(data);

            var clientId = Math.floor(Math.random() * 100);
            $.ajax({
                url: api.Toast.Create,
                type: "POST",
                data: JSON.stringify({
                    "Title": "Google Authentication",
                    "Message": "The selected authentication has been revoked from Google.",
                    "ClientId": clientId,
                    "Colour": "success"
                }),
                contentType: "application/json",
                cache: false,
                async: false,
                success: function (data) {
                    $('#toaster').append(data);
                    $('#' + clientId).toast('show');
                }
            });
        }
    });
});
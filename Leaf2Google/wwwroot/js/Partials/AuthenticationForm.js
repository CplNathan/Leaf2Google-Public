function onSubmit(token) {
    $('#Captcha').val(token);
    $('form')[0].submit();
}

function onExpire() {
    var clientId = Math.floor(Math.random() * 100);
    $.ajax({
        url: api.toaster,
        type: "POST",
        data: JSON.stringify({
            "Title": "Google reCaptcha",
            "Message": "The reCaptcha form timed out, please try again.",
            "ClientId": clientId,
            "Colour": "warning"
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

function onError() {
    var clientId = Math.floor(Math.random() * 100);
    $.ajax({
        url: api.toaster,
        type: "POST",
        data: JSON.stringify({
            "Title": "Google reCaptcha",
            "Message": "There was an error verifying the reCaptcha.",
            "ClientId": clientId,
            "Colour": "error"
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
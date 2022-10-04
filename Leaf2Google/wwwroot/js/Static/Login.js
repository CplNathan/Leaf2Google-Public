$("form").validate({
    rules: {
        NissanUsername: {
            required: true,
            email: true,
            /*
            remote: {
                url: "/Validation/UsernameUnique",
                type: "post",
                data: {
                    Username: function () {
                        return $("#NissanUsername").val();
                    }
                }
            }
            */
        },
        NissanPassword: {
            required: true
        }
    },
    messages: {
        NissanUsername: {
            remote: "Please enter a unique email address."
        },
        NissanPassword: {
            required: "Please enter a valid password."
        }
    },
    onfocusout: function (element) {
        this.element(element);
    },
    errorPlacement: $.noop,
    submitHandler: function (form) {
        grecaptcha.execute();
    }
});

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
            "Message": "The reCAPTCHA form timed out, please try again.",
            "ClientId": clientId,
            "Colour": "warning"
        }),
        contentType: "application/json",
        cache: false,
        async: true,
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
            "Message": "There was an error verifying the reCAPTCHA.",
            "ClientId": clientId,
            "Colour": "error"
        }),
        contentType: "application/json",
        cache: false,
        async: true,
        success: function (data) {
            $('#toaster').append(data);
            $('#' + clientId).toast('show');
        }
    });
}
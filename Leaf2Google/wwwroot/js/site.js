$(document).ready(function () {
    $('.toast').toast('show');
});
$('#mapbox').on('load', function () {
    mapboxgl.accessToken = '@(ViewBag?.MapBoxKey)';
});
$(document).ready(function () {
    function checkForInput(element) {
        if ($(element).val().length > 0) {
            $(element).addClass('active');
        } else {
            $(element).removeClass('active');
        }
    }

    $('.form-control').each(function () {
        checkForInput(this);
    });

    $('.form-control').on('change keyup', function () {
        checkForInput(this);
    });

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
});
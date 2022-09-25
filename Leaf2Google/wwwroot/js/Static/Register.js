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
    errorPlacement: function (error, element) {
        this.element(element);
        //$('#errors').append(error);
    },
    submitHandler: function (form) {
        form.submit();
    }
});
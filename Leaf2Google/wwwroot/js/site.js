function updatePanels() {
    $('l2g-chargestatus').each(function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        $.ajax({
            url: api.Car.Status,
            type: "POST",
            data: JSON.stringify({
                "query": "battery",
                "vin": vin
            }),
            contentType: "application/json",
            cache: false,
            async: false,
            success: function (data) {
                control.attr('percentage', data.percentage);
                control.attr('charging', data.charging);
            }
        });
    })
}

function checkForInput(element) {
    if ($(element).val().length > 0) {
        $(element).addClass('active');
    } else {
        $(element).removeClass('active');
    }
}

$(document).ready(function () {
    $('.toast').toast('show');

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

    setInterval(updatePanels, 5000);
});
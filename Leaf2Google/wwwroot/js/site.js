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

    $('l2g-locationmap').each(function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        $.ajax({
            url: api.Car.Status,
            type: "POST",
            data: JSON.stringify({
                "query": "location",
                "vin": vin
            }),
            contentType: "application/json",
            cache: false,
            async: false,
            success: function (data) {
                control.attr('lat', data.lat);
                control.attr('long', data.long);
            }
        });
    })

    $('l2g-climatecurrent').each(function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        $.ajax({
            url: api.Car.Status,
            type: "POST",
            data: JSON.stringify({
                "query": "climate",
                "vin": vin
            }),
            contentType: "application/json",
            cache: false,
            async: false,
            success: function (data) {
                control.attr('current', data.current);
            }
        });
    })

    $('l2g-climatetarget').each(function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        $.ajax({
            url: api.Car.Status,
            type: "POST",
            data: JSON.stringify({
                "query": "climate",
                "vin": vin
            }),
            contentType: "application/json",
            cache: false,
            async: false,
            success: function (data) {
                control.attr('target', data.target);
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
    $('.form-control').each(function () {
        checkForInput(this);
    });

    $('.form-control').on('change keyup', function () {
        checkForInput(this);
    });

    $('.toast').toast('show');
    setInterval(updatePanels, 5000);
});
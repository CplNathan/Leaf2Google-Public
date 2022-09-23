            $('#vehicleControls button').on('click', function () {
                var action = $(this).attr('data-itemid');
                var duration = $('#vehicleControls input').val();

                $.ajax({
                    url: api.car.action + "/?action=" + action + "&duration=" + duration,
                    type: "POST",
                    data: JSON.stringify({ action: action }),
                    contentType: "application/json",
                    cache: false,
                    async: false,
                    success: function (data) {
                        $('#sessionWrapper').html(data);

                        var clientId = Math.floor(Math.random() * 100);
                        $.ajax({
                            url: api.toaster,
                            type: "POST",
                            data: JSON.stringify({
                                "Title": "Nissan Action",
                                "Message": "The action '" + action + "' has been sent.",
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
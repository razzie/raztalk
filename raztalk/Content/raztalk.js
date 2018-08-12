$(document).ready(function () {
    var token = $("body").data("token");
    var channel = $.connection.channelHub;

    channel.client.send = function (user, message, timestamp) {
        row = "<tr><td>" + user + "</td><td data-timestamp=\"" + timestamp + "\">" + message + "</td></tr>";
        $("#messages tr:last").after(row);
    };
    channel.client.sendInfo = function (info) {
        row = "<tr><td></td><td>" + info + "</td></tr>";
        $("#messages tr:last").after(row);
    };

    $.connection.hub.start().done(function () {
        channel.server.login(token);

        $("#message").keypress(function (e) {
            if (e.which == 13) {
                msg = $(this);
                channel.server.send(msg.val());
                msg.val("");
            }
        });
    });

    window.sr = ScrollReveal({ reset: true });
    sr.reveal('.reveal');
});

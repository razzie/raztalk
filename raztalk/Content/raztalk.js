$(document).ready(function () {
    var token = $("body").data("token");
    var channel = $.connection.channelHub;

    function linkify(str) {
        return str.replace(/(<a href=")?((https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)))(">(.*)<\/a>)?/gi, function () {

            return '<a href="' + arguments[2] + '">' + (arguments[7] || arguments[2]) + '</a>'
        });
    }

    channel.client.send = function (user, message, timestamp) {
        row = "<tr><td>" + user + "</td><td data-timestamp=\"" + timestamp + "\">" + linkify(message) + "</td></tr>";
        $("#messages tr:last").after(row);
        $("#messages tr:last a").oembed();
    };
    channel.client.sendInfo = function (info) {
        row = "<tr><td></td><td>" + info + "</td></tr>";
        $("#messages tr:last").after(row);
        $("#messages tr:first").fadeOut("slow");
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

    $("td").each(function () {
        item = $(this);
        item.html(linkify(item.text()));
    });

    $("a").oembed();

    window.sr = ScrollReveal({ reset: true });
    sr.reveal('.reveal');
});

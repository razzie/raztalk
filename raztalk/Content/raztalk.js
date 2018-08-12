$(document).ready(function () {
    var users_visible = true;
    var token = $("body").data("token");
    var channel = $.connection.channelHub;

    function linkify(str) {
        return str.replace(/(<a href=")?((https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)))(">(.*)<\/a>)?/gi, function () {

            return '<a href="' + arguments[2] + '">' + (arguments[7] || arguments[2]) + '</a>'
        });
    }

    function autoscroll() {
        $("html, body").animate({ scrollTop: $(document).height() }, 1000);
    }

    channel.client.send = function (user, message, timestamp) {
        row = "<tr class=\"reveal\"><td>" + user + "</td><td data-timestamp=\"" + timestamp + "\">" + linkify(message) + "</td></tr>";
        $("#messages tr:last").after(row);
        $("#messages tr:last a").oembed();
        autoscroll();
    };
    channel.client.sendInfo = function (info) {
        row = "<tr class=\"reveal\"><td></td><td>" + info + "</td></tr>";
        $("#messages tr:last").after(row);
        if (users_visible) {
            $("#users").fadeOut("slow");
            users_visible = false;
        }
        autoscroll();
    };

    $.connection.hub.start().done(function () {
        channel.server.login(token);

        $("#message").keypress(function (e) {
            if (e.which == 13) {
                msg = $(this);
                channel.server.send(msg.val());
                msg.val("");
                msg.focus();
            }
        });
    });

    $("td").each(function () {
        item = $(this);
        item.html(linkify(item.text()));
    });

    $("a").oembed();

    $("#message").focus();

    window.sr = ScrollReveal({ reset: true });
    sr.reveal('.reveal');
});

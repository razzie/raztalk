$(document).ready(function () {
    var isActive = true;
    var unread = 0;
    var username = $("body").data("user");
    var channelname = $("body").data("channel");
    var channel = $.connection.channelHub;
    var lastMsgUser = "";
    var lastMsg = "";
    var lastMsgTimestamp = 0;
    var firstMsgSent = false;

    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, "");
    };
    Date.prototype.razformat = function () {
        var year = this.getFullYear();
        var month = ("0" + (this.getMonth() + 1)).slice(-2);
        var day = ("0" + this.getDate()).slice(-2);
        var hour = ("0" + this.getHours()).slice(-2);
        var minute = ("0" + this.getMinutes()).slice(-2);
        var second = ("0" + this.getSeconds()).slice(-2);
        return year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;
    }

    function fileExtension(url) {
        return (url = url.substr(1 + url.lastIndexOf("/")).split('?')[0]).split('#')[0].substr(url.lastIndexOf(".")).toLowerCase();
    }
    function isImage(ext) {
        var image_types = [".jpg", ".jpeg", ".png", ".gif", ".gifv"];
        return (image_types.indexOf(ext) != -1);
    }
    function isAudio(ext) {
        var audio_types = [".mp3", ".ogg"];
        return (audio_types.indexOf(ext) != -1);
    }
    function isVideo(ext) {
        var media_types = [".mp4", ".webm"];
        return (media_types.indexOf(ext) != -1);
    }

    function formatUser(user) {
        if (user == lastMsgUser) {
            return "";
        } else if (user == username && firstMsgSent) {
            return "<strong>" + user + "</strong>";
        } else {
           return user;
        }
    }
    function scrollDown() {
        $("html, body").scrollTop($(document).height());
    }
    function displayMsg(user, message, timestamp) {
        var elapsedMs = timestamp - lastMsgTimestamp;
        if (elapsedMs > 120000) {
            if (elapsedMs > 600000) { // timestamp bar after 10 mins
                lastMsgUser = "";
                var ts = new Date(timestamp)
                var separator = "<tr><td colspan=\"2\" class=\"text-right\"><small>" + ts.razformat() + "</small><hr /></td></tr>";
                $("#messages tr:last").after(separator);
            } else { // spacing after 2 mins (but less than 10)
                var spacing = "<tr><td colspan=\"2\">&nbsp;</td></tr>";
                $("#messages tr:last").after(spacing);
            }
        }

        var row;
        if (user == "") {
            row = "<tr class=\"info reveal\"><td></td><td data-timestamp=\"" + timestamp + "\"><pre>" + message + "</pre></td></tr>";
        } else {
            row = "<tr class=\"reveal\"><td>" + formatUser(user) + "</td><td data-timestamp=\"" + timestamp + "\"><pre>" + message + "</pre></td></tr>";
        }

        $("#messages tr:last").after(row);
        msg = $("#messages tr:last pre");

        msg.linkify();
        links = msg.find("a");
        links.oembed();
        links.each(function () {
            var url = $(this).attr("href");
            var ext = fileExtension(url);

            if (isImage(ext)) {
                $(this).html(url + "<br /><img src=\"" + url + "\" />");
            }
            else if (isAudio(ext)) {
                $(this).html(url + "<br /><audio controls><source src=\"" + url + "\" /></audio>");
            }
            else if (isVideo(ext)) {
                $(this).html(url + "<br /><video controls><source src=\"" + url + "\" /></video>");
            }
        });

        sr.reveal('.reveal');

        scrollDown();
        msg.find("img, video, audio").on("load", function () {
            scrollDown();
        });

        if (!isActive) {
            unread += 1;
            document.title = "(+" + unread + ") RazTalk - " + channelname;
            $.playSound("/content/notification.mp3");
        }

        lastMsgUser = user;
        lastMsg = message;
        lastMsgTimestamp = timestamp;
    }
    function errorMsg(err) {
        displayMsg("", err, lastMsgTimestamp)
    }

    function reconnect() {
        $.connection.hub.start().done(function () {
            channel.client.requestLogin();
        });
    }

    $(window).focus(function (event) {
        isActive = true;
        unread = 0;
        document.title = "RazTalk - " + channelname;
    });
    $(window).blur(function (event) {
        isActive = false;
    });

    channel.client.send = function (user, message, timestamp) {
        if (timestamp < lastMsgTimestamp) return;
        if (timestamp == lastMsgTimestamp && message == lastMsg && user == lastMsgUser) return;
        displayMsg(user, message, timestamp);
    };
    channel.client.updateUsers = function (users) {
        $("#users").text("Connected users: " + users);
    }
    channel.client.requestLogin = function () {
        body = $("body");
        if (channel.server.login(body.data("user"), body.data("channel"), body.data("pw"), lastMsgTimestamp)) {
            $("#message").prop("disabled", false).focus();
            $(".reconnect").removeAttr("href");
        } else {
            errorMsg("Login failed");
        }
    }

    $.connection.hub.start().done(function () {
        token = $("body").data("token");

        if (channel.server.join(token)) {
            $("#message").prop("disabled", false).focus();
        } else {
            errorMsg("Join failed");
        }

        $("#message").keypress(function (e) {
            if (e.which == 13 && !e.shiftKey) {
                e.preventDefault();
                msg = $(this);
                if (msg.val().length > 0) {
                    channel.server.send(msg.val().trim());
                    firstMsgSent = true;
                }
                msg.val("");
                msg.focus();
            }
        });
    });
    $.connection.hub.disconnected(function () {
        $("#message").prop("disabled", true);
        errorMsg("Disconnected ( <a href=\"#\" class=\"reconnect\" target=\"_self\">Reconnect</a> )");
        $(".reconnect").click(reconnect);
    });

    history.pushState({}, "", "/channel/" + channelname);

    sr.reveal('.reveal');
});

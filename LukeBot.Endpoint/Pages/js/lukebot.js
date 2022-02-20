const usersURI = "api/Users"

const user = {
    name: "",
    displayName: ""
};

const DARK_MODE_COOKIE = "darkmode";
const OAUTH2_CLIENT_ID = "oj37p7c4qrlvmvugm9i6ytn9q55prk";

let appbarElement = null;
let usersDropdownMenuElement = null;
let usersMainMenuElement = null;
let addUserUsernameInputElement = null;
let addUserDisplayNameInputElement = null;

let popupWindow = null;

let users = [];


function _AddUserToDropDownMenu(userObject) {
    let entry = document.createElement("li");
    let entryContents = document.createElement("a");
    entryContents.setAttribute("href", userObject.name);
    entryContents.text = userObject.displayName;
    entry.appendChild(entryContents);
    usersDropdownMenuElement.appendChild(entry);
}

function _AddUserToMainTable(userObject) {
    if (usersMainMenuElement == null)
        return;

    let entry = document.createElement("li");
    let entryContents = document.createElement("a");
    entryContents.setAttribute("href", userObject.name);
    entryContents.text = userObject.displayName;
    entry.appendChild(entryContents);
    usersMainMenuElement.appendChild(entry);
}

function _AddUser(name, displayName) {
    let u = Object.create(user);
    u.name = name;
    u.displayName = displayName;
    console.log("_AddUser() " + u);
    users.push(u);
}

function _ProcessUsersResponse(usersResponse) {
    if (usersResponse.status != 0) {
        alert('Invalid status in response: ' + usersResponse.status);
    }

    usersResponse.users.forEach((user) => {
        console.log(user);
        users.push(user);
    });
}


function GetUsers() {
    return fetch(usersURI)
        .then(response => response.json())
        .then(data => _ProcessUsersResponse(data));
}

function AddUser() {
    // TODO sanitize
    let name = addUserUsernameInputElement.value;
    let displayName = addUserDisplayNameInputElement.value;
    console.log("Adding user " + name + ' with display name ' + displayName);
    _AddUser(name, displayName);

    // Redirect to Twitch OAuth
    let url = "https://id.twitch.tv/oauth2/authorize"
        + "?client_id=" + OAUTH2_CLIENT_ID
        + "&redirect_uri=http://localhost:5000/callback/twitch"
        + "&response_type=code"
        + "&scope=moderation:read"
        + "&force_verify=true"
        + "&state=lmaool";
    popupWindow = window.open(url);
}

function AddUsersToUI() {
    users.forEach((user) => {
        _AddUserToDropDownMenu(user);
        _AddUserToMainTable(user);
    })
}

function DarkModeToggle() {
    var bodyClassList = document.body.classList;
    bodyClassList.toggle("body-dark-mode");

    if (appbarElement != null) {
        appbarElement.classList.toggle("bg-appbar-dark-mode");
    }

    if (usersMainMenuElement != null) {
        usersMainMenuElement.classList.toggle("bg-main-menu-dark-mode");
    }

    Cookies.set(DARK_MODE_COOKIE, bodyClassList.contains('body-dark-mode'));
}

function ClosePopupWindow() {
    if (popupWindow == null) {
        alert("ClosePopupWindow() shouldn't be called! popupWindow is null");
    }

    popupWindow.close();
    popupWindow = null;
}


$(document).ready(function () {
    var includes = $('[data-include]');
    var promises = [];
    $.each(includes, function (i, obj) {
        var file = '/views/' + $(this).data('include') + '.html';
        promises.push(
            fetch(file)
                .then((response) => response.text())
                .then((html) => {
                    obj.innerHTML = html;
                    console.log("Included file " + file);
                })
        );
    });

    promises.push(GetUsers());

    Promise
        .all(promises)
        .then(() => {
            appbarElement = document.getElementById("lukebot-appbar");
            usersDropdownMenuElement = document.getElementById("lukebot-users-dropdown");
            usersMainMenuElement = document.getElementById("lukebot-users-main-menu");
            addUserUsernameInputElement = document.getElementById("lukebot-adduser-username");
            addUserDisplayNameInputElement = document.getElementById("lukebot-adduser-displayname");

            AddUsersToUI();

            var darkmode = Cookies.get(DARK_MODE_COOKIE);
            if (darkmode) {
                if (darkmode == 'true') {
                    DarkModeToggle();
                }
            } else {
                Cookies.set(DARK_MODE_COOKIE, 'false');
            }
        });
});

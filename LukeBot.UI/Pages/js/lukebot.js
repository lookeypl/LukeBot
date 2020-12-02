const usersURI = "api/Users"

const user = {
    name: "",
    displayName: ""
};
let usersDropdownMenu = null;
let addUserUsernameInput = null;
let addUserDisplayNameInput = null;
let users = [];

function GetUsers() {
    fetch(usersURI)
        .then(response => response.json())
        .then(data => _ProcessUsers(data))
        .catch(error => console.error('Unable to get items.', error));
}

function _ProcessUsers(usersData) {
    for (const u of users) {
        _AddUserToDropDownMenu(u);
    }
}

function AddUser() {
    // TODO sanitize
    let name = addUserUsernameInput.value;
    let displayName = addUserDisplayNameInput.value;
    console.log("Adding user " + name + ' with display name ' + displayName);
    _AddUser(name, displayName);
}

function _AddUser(name, displayName) {
    let u = Object.create(user);
    u.name = addUserUsernameInput.value;
    u.displayName = addUserDisplayNameInput.value;
    users.push(u);
    _AddUserToDropDownMenu(u);
}

function _AddUserToDropDownMenu(user) {
    let entry = document.createElement("li");
    let entryContents = document.createElement("a");
    entryContents.setAttribute("href", user.name);
    entryContents.text = user.displayName;
    entry.appendChild(entryContents);
    usersDropdownMenu.appendChild(entry);
}


const DARK_MODE_COOKIE = "darkmode";

function DarkModeToggle() {
    var bodyClassList = document.body.classList;
    bodyClassList.toggle("dark-mode");
    document.getElementById("lukebot-appbar").classList.toggle("bg-appbar-dark-mode");
    Cookies.set(DARK_MODE_COOKIE, bodyClassList.contains('dark-mode'));
}

$(document).ready(function () {
    var includes = $('[data-include]');
    var promises = [];
    $.each(includes, function (i, obj) {
        var file = 'views/' + $(this).data('include') + '.html';
        promises.push(
            fetch(file)
                .then((response) => response.text())
                .then((html) => {
                    obj.innerHTML = html;
                    console.log("Included file " + file);
                })
        );
    });

    Promise
        .all(promises)
        .then(() => {
            var darkmode = Cookies.get(DARK_MODE_COOKIE);
            if (darkmode) {
                if (darkmode == 'true') {
                    DarkModeToggle();
                }
            } else {
                Cookies.set(DARK_MODE_COOKIE, 'false');
            }

            usersDropdownMenu = document.getElementById("lukebot-users-dropdown");
            addUserUsernameInput = document.getElementById("lukebot-adduser-username");
            addUserDisplayNameInput = document.getElementById("lukebot-adduser-displayname");
        });
});

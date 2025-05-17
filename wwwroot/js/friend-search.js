document.addEventListener('DOMContentLoaded', function () {
    // Hem navbar hem footer için ayrı ayrı input ve results box bul
    const inputs = [
        { input: document.getElementById('friend-search-input-navbar'), results: document.getElementById('friend-search-results-navbar') },
        { input: document.getElementById('friend-search-input-footer'), results: document.getElementById('friend-search-results-footer') }
    ];

    inputs.forEach(({ input, results }) => {
        if (!input || !results) return;

        input.addEventListener('input', function () {
            const query = input.value.trim();
            if (query.length < 2) {
                results.style.display = 'none';
                results.innerHTML = '';
                return;
            }
            fetch(`/Dashboard/SearchUsers?query=${encodeURIComponent(query)}`)
                .then(res => res.json())
                .then(data => {
                    if (data.success && data.users.length > 0) {
                        results.innerHTML = data.users.map(user => `
                            <div class="friend-search-result">
                                <span class="friend-info">
                                    <img src="${user.profileImageUrl || '/images/default-avatar.png'}" class="friend-avatar">
                                    <span class="friend-name">${user.firstName} ${user.lastName}</span>
                                    <span class="friend-status">
                                        <span class="status-dot" style="background:${user.isOnline ? '#4CAF50' : '#ff4d4d'}"></span>
                                        <span style="color:${user.isOnline ? '#4CAF50' : '#ff4d4d'};font-size:0.9em;">
                                            ${user.isOnline ? 'Online' : 'Offline'}
                                        </span>
                                    </span>
                                </span>
                                ${user.isFriend
                                ? `<button class=\"btn btn-sm btn-secondary\" disabled style=\"opacity:0.85;cursor:default;min-width:110px\">Arkadaşınız</button>`
                                : user.requested
                                    ? `<button class=\"friend-requested-btn\" onclick=\"cancelFriendRequest(${user.id}, this)\">İstek Gönderildi</button>`
                                    : `<button class=\"btn btn-sm btn-primary friend-add-btn\" onclick=\"addFriend(${user.id}, this)\"><span style=\"font-size:1.2em;\">+</span></button>`
                            }
                            </div>
                        `).join('');
                        results.style.display = 'block';
                    } else {
                        results.innerHTML = '<div style="padding:8px;">Kullanıcı bulunamadı.</div>';
                        results.style.display = 'block';
                    }
                });
        });
    });

    window.addFriend = function (userId, btn) {
        btn.disabled = true;
        btn.innerHTML = '...';
        fetch('/Dashboard/AddFriend', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ friendId: userId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    btn.outerHTML = `<button class="friend-requested-btn" onclick="cancelFriendRequest(${userId}, this)">İstek Gönderildi</button>`;
                } else {
                    btn.textContent = 'Hata';
                    btn.disabled = false;
                }
            });
    };

    window.cancelFriendRequest = function (userId, btn) {
        btn.disabled = true;
        btn.innerHTML = '...';
        fetch('/Dashboard/CancelFriendRequest', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ friendId: userId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    btn.outerHTML = `<button class="btn btn-sm btn-primary friend-add-btn" onclick="addFriend(${userId}, this)"><span style="font-size:1.2em;">+</span></button>`;
                } else {
                    btn.textContent = 'Hata';
                    btn.disabled = false;
                }
            });
    };
});

document.addEventListener('click', function (event) {
    const inputs = [
        { input: document.getElementById('friend-search-input-navbar'), results: document.getElementById('friend-search-results-navbar') },
        { input: document.getElementById('friend-search-input-footer'), results: document.getElementById('friend-search-results-footer') }
    ];
    inputs.forEach(({ input, results }) => {
        if (!input || !results) return;
        if (!input.contains(event.target) && !results.contains(event.target)) {
            results.style.display = 'none';
        }
    });
});

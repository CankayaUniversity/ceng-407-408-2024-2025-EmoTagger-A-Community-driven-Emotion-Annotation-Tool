// Online durumunu güncellemek için fonksiyon
function updateOnlineStatus(isOnline) {
    fetch('/Dashboard/UpdateOnlineStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ isOnline: isOnline })
    })
    .then(response => response.json())
    .then(data => {
        if (!data.success) {
            console.error('Online durumu güncellenirken hata oluştu:', data.message);
        }
    })
    .catch(error => {
        console.error('Online durumu güncellenirken hata oluştu:', error);
    });
}

// Online kullanıcıları getirmek için fonksiyon
function getOnlineUsers() {
    fetch('/Dashboard/GetOnlineUsers')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateOnlineUsersList(data.onlineUsers);
            } else {
                console.error('Online kullanıcılar alınırken hata oluştu:', data.message);
            }
        })
        .catch(error => {
            console.error('Online kullanıcılar alınırken hata oluştu:', error);
        });
}

// Online kullanıcı listesini güncellemek için fonksiyon
function updateOnlineUsersList(onlineUsers) {
    const onlineUsersContainer = document.getElementById('online-users');
    if (!onlineUsersContainer) return;

    onlineUsersContainer.innerHTML = '';
    
    onlineUsers.forEach(user => {
        const userElement = document.createElement('div');
        userElement.className = 'online-user';
        userElement.innerHTML = `
            <img src="${user.profileImageUrl || '/images/default-avatar.png'}" alt="${user.firstName} ${user.lastName}" class="user-avatar">
            <span class="user-name">${user.firstName} ${user.lastName}</span>
            <span class="online-indicator"></span>
        `;
        onlineUsersContainer.appendChild(userElement);
    });
}

// Sayfa yüklendiğinde ve her 30 saniyede bir online kullanıcıları güncelle
document.addEventListener('DOMContentLoaded', () => {
    // Kullanıcı online olarak işaretle
    updateOnlineStatus(true);
    
    // İlk online kullanıcıları getir
    getOnlineUsers();
    
    // Her 30 saniyede bir online kullanıcıları güncelle
    setInterval(getOnlineUsers, 30000);
    
    // Sayfa kapatıldığında kullanıcıyı offline yap
    window.addEventListener('beforeunload', () => {
        updateOnlineStatus(false);
    });
}); 
function updateOnlineStatus(isOnline) {
    fetch('/Dashboard/UpdateOnlineStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ isOnline: isOnline })
    })
    .then(response => response.json())
    .then(data => {
        if (!data.success) {
            console.error('Online durumu güncellenirken hata oluştu:', data.message);
        }
    })
    .catch(error => {
        console.error('Online durumu güncellenirken hata oluştu:', error);
    });
}

// Online kullanıcıları getirmek için fonksiyon
function getOnlineUsers() {
    fetch('/Dashboard/GetOnlineUsers')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateOnlineUsersList(data.onlineUsers);
            } else {
                console.error('Online kullanıcılar alınırken hata oluştu:', data.message);
            }
        })
        .catch(error => {
            console.error('Online kullanıcılar alınırken hata oluştu:', error);
        });
}

// Online kullanıcı listesini güncellemek için fonksiyon
function updateOnlineUsersList(onlineUsers) {
    const onlineUsersContainer = document.getElementById('online-users');
    if (!onlineUsersContainer) return;

    onlineUsersContainer.innerHTML = '';
    
    onlineUsers.forEach(user => {
        const userElement = document.createElement('div');
        userElement.className = 'online-user';
        userElement.innerHTML = `
            <img src="${user.profileImageUrl || '/images/default-avatar.png'}" alt="${user.firstName} ${user.lastName}" class="user-avatar">
            <span class="user-name">${user.firstName} ${user.lastName}</span>
            <span class="online-indicator"></span>
        `;
        onlineUsersContainer.appendChild(userElement);
    });
}

// Sayfa yüklendiğinde ve her 30 saniyede bir online kullanıcıları güncelle
document.addEventListener('DOMContentLoaded', () => {
    // Kullanıcı online olarak işaretle
    updateOnlineStatus(true);
    
    // İlk online kullanıcıları getir
    getOnlineUsers();
    
    // Her 30 saniyede bir online kullanıcıları güncelle
    setInterval(getOnlineUsers, 30000);
    
    // Sayfa kapatıldığında kullanıcıyı offline yap
    window.addEventListener('beforeunload', () => {
        updateOnlineStatus(false);
    });
}); 
 
 
 
 
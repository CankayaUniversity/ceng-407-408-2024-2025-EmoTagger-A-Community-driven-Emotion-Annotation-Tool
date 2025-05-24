#!/bin/bash

# Gerekli paketleri yükle
sudo apt-get update
sudo apt-get install -y python3-venv nginx

# Proje dizinini oluştur
sudo mkdir -p /var/www/emotagger
sudo chown -R www-data:www-data /var/www/emotagger

# Python sanal ortam oluştur
cd /var/www/emotagger
python3 -m venv venv
source venv/bin/activate

# Gerekli Python paketlerini yükle
pip install fastapi uvicorn gunicorn python-multipart

# Servis dosyasını kopyala
sudo cp emotagger.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable emotagger
sudo systemctl start emotagger

# Nginx yapılandırması
sudo tee /etc/nginx/sites-available/emotagger << EOF
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:8000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
    }
}
EOF

# Nginx site'ini etkinleştir
sudo ln -s /etc/nginx/sites-available/emotagger /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx 
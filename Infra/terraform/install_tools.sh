#!/bin/bash
set -euo pipefail

apt-get update
apt-get install -y docker.io docker-compose awscli

mkdir -p /app
cd /app

cat > docker-compose.yml <<'EOF'
version: "3.8"
services:
  meilisearch:
    image: getmeili/meilisearch:latest
    environment:
      - MEILI_MASTER_KEY=${MEILI_MASTER_KEY}
    volumes:
      - meili_data:/data
    restart: always

  loki:
    image: grafana/loki:latest
    ports:
      - "3100:3100"
    restart: always

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}
    restart: always

volumes:
  meili_data:
EOF

echo "MEILI_MASTER_KEY=$(aws ssm get-parameter --name \"/everland/meili_key\" --with-decryption --query \"Parameter.Value\" --output text)" > .env
echo "GRAFANA_ADMIN_PASSWORD=$(aws ssm get-parameter --name \"/everland/grafana_admin_password\" --with-decryption --query \"Parameter.Value\" --output text)" >> .env

docker-compose up -d

wget -q https://s3.amazonaws.com/amazoncloudwatch-agent/ubuntu/amd64/latest/amazon-cloudwatch-agent.deb
dpkg -i -E ./amazon-cloudwatch-agent.deb
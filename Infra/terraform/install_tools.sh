#!/bin/bash
set -euo pipefail

apt-get update
apt-get install -y docker.io docker-compose awscli jq

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

SECRETS_JSON=$(aws secretsmanager get-secret-value --secret-id "e-verland/secret-key" --query "SecretString" --output text)

echo "MEILI_MASTER_KEY=$(echo '$SECRETS_JSON' | jq -r '.Meilisearch.MasterKey')" > .env
echo "GRAFANA_ADMIN_PASSWORD=$(echo '$SECRETS_JSON' | jq -r '.Grafana.AdminPassword')" >> .env
docker-compose up -d

wget -q https://s3.amazonaws.com/amazoncloudwatch-agent/ubuntu/amd64/latest/amazon-cloudwatch-agent.deb
dpkg -i -E ./amazon-cloudwatch-agent.deb
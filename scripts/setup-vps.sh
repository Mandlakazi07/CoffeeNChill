#!/usr/bin/env bash
# CODE ATTRIBUTION (IIE Harvard Anglia). Full reference list: docs/CODE_ATTRIBUTION.md
# (Docker, n.d.-h)
#   Title:    Install Docker Engine on Ubuntu
#   Author:   Docker Inc.
#   Date:     n.d. (accessed 10 November 2026)
#   Version:  online documentation, no version number
#   Link:     https://docs.docker.com/engine/install/ubuntu/
#   Used for: installing Docker Engine and the Compose plugin from Docker's apt repository.
# (Docker, n.d.-i)
#   Title:    Linux post-installation steps for Docker Engine
#   Author:   Docker Inc.
#   Date:     n.d. (accessed 10 November 2026)
#   Version:  online documentation, no version number
#   Link:     https://docs.docker.com/engine/install/linux-postinstall/
#   Used for: the docker group and starting Docker on boot.
#
# One-off preparation of an Ubuntu VPS for CoffeeNChill (Part 3).
#
#   1. Installs Docker Engine + the Compose plugin and starts Docker on boot.
#   2. Lets the current user run Docker without sudo (needed by the CI deploy job).
#   3. Turns on the UFW firewall with only SSH and the gateway port open.
#   4. Creates ~/coffeenchill/.env with a random JWT key.
#
# Usage (on the VPS, as the user that will run the app, not as root):
#   ADMIN_EMAIL=you@example.com ADMIN_PASSWORD='a-long-password' bash setup-vps.sh
#
# Also open the same port (default 5000) in the cloud provider's firewall / security group.
# Safe to run more than once.

set -euo pipefail

GATEWAY_PORT="${GATEWAY_PORT:-5000}"
APP_DIR="${APP_DIR:-$HOME/coffeenchill}"

if [ "$(id -u)" -eq 0 ]; then
  echo "Run this as the normal user that will run the app (it uses sudo where needed), not as root." >&2
  exit 1
fi

if [ -z "${ADMIN_EMAIL:-}" ] || [ -z "${ADMIN_PASSWORD:-}" ]; then
  echo "Set ADMIN_EMAIL and ADMIN_PASSWORD, for example:" >&2
  echo "  ADMIN_EMAIL=you@example.com ADMIN_PASSWORD='a-long-password' bash setup-vps.sh" >&2
  exit 1
fi

if [ "${#ADMIN_PASSWORD}" -lt 8 ]; then
  echo "ADMIN_PASSWORD must be at least 8 characters." >&2
  exit 1
fi

echo "==> 1. Docker"
if ! command -v docker > /dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y ca-certificates curl
  sudo install -m 0755 -d /etc/apt/keyrings
  sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  sudo chmod a+r /etc/apt/keyrings/docker.asc
  # shellcheck disable=SC1091
  codename="$(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")"
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $codename stable" \
    | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
  sudo apt-get update
  sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
else
  echo "Docker is already installed: $(docker --version)"
fi

# Start Docker now and on every boot. Together with "restart: unless-stopped" in
# docker-compose.yml, this brings the containers back after the server restarts.
sudo systemctl enable --now docker containerd

echo "==> 2. docker group"
if ! id -nG "$USER" | grep -qw docker; then
  sudo usermod -aG docker "$USER"
  echo "Added $USER to the docker group. Log out and back in (or run 'newgrp docker') before using docker without sudo."
fi

echo "==> 3. Firewall (UFW)"
sudo apt-get install -y ufw
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow OpenSSH
sudo ufw allow "${GATEWAY_PORT}/tcp"
sudo ufw --force enable
sudo ufw status verbose
# Docker publishes ports through its own iptables rules. Azurite (11000-11002) and the
# Functions app (18080) are bound to 127.0.0.1 in docker-compose.yml, so only the gateway
# port is reachable from outside.

echo "==> 4. App folder and .env"
mkdir -p "$APP_DIR"
if [ -f "$APP_DIR/.env" ]; then
  echo "$APP_DIR/.env already exists, leaving it as it is."
else
  umask 077
  cat > "$APP_DIR/.env" <<EOF
JWT_KEY=$(openssl rand -base64 48)
JWT_ISSUER=CoffeeNChillGateway
JWT_AUDIENCE=CoffeeNChillClients
ADMIN_EMAIL=${ADMIN_EMAIL}
ADMIN_PASSWORD=${ADMIN_PASSWORD}
DOCKERHUB_NAMESPACE=mandlakazi07
FUNCTIONS_TAG=part2
GATEWAY_TAG=part3
GATEWAY_PORT=${GATEWAY_PORT}
EOF
  echo "Created $APP_DIR/.env (readable only by $USER)."
fi

cat <<EOF

Done. Next steps:
  - Open TCP port ${GATEWAY_PORT} in the cloud provider's firewall as well.
  - Either let the GitHub Actions deploy job copy docker-compose.yml here, or copy it
    yourself and run:  cd $APP_DIR && docker compose pull && docker compose up -d
  - Check:  curl http://localhost:${GATEWAY_PORT}/swagger/v1/swagger.json
  - Then open http://<this-server's-public-ip>:${GATEWAY_PORT}/swagger from another network
    (for example a phone on mobile data).
EOF

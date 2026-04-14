#!/usr/bin/env bash
set -euo pipefail

REPO="AdamTovatt/database-mcp"
BINARY_NAME="db"
INSTALL_DIR="/usr/local/bin"

check_dependencies() {
    if ! command -v gh &>/dev/null; then
        echo "Error: GitHub CLI (gh) is required. Install it from https://cli.github.com/" >&2
        exit 1
    fi

    if ! gh auth status &>/dev/null; then
        echo "Error: Not authenticated with GitHub CLI. Run 'gh auth login' first." >&2
        exit 1
    fi
}

detect_asset() {
    local os arch
    os="$(uname -s)"
    arch="$(uname -m)"

    case "$os" in
        Darwin) os="osx" ;;
        Linux)  os="linux" ;;
        *)      echo "Unsupported OS: $os" >&2; exit 1 ;;
    esac

    case "$arch" in
        x86_64|amd64)  arch="x64" ;;
        arm64|aarch64) arch="arm64" ;;
        *)             echo "Unsupported architecture: $arch" >&2; exit 1 ;;
    esac

    echo "db-${os}-${arch}.zip"
}

get_latest_version() {
    gh api "repos/${REPO}/releases" --jq '[.[] | select(.tag_name | startswith("v"))][0].tag_name' \
        | sed 's/^v//'
}

get_installed_version() {
    if command -v "$BINARY_NAME" &>/dev/null; then
        "$BINARY_NAME" --version 2>/dev/null || echo ""
    else
        echo ""
    fi
}

main() {
    check_dependencies

    echo "Detecting platform..."
    local asset
    asset="$(detect_asset)"
    echo "  Asset: $asset"

    echo "Fetching latest version..."
    local version
    version="$(get_latest_version)"
    if [ -z "$version" ]; then
        echo "Failed to determine latest version." >&2
        exit 1
    fi
    echo "  Latest: $version"

    local installed
    installed="$(get_installed_version)"
    if [ "$installed" = "$version" ]; then
        echo "Already up to date ($version)."
        exit 0
    fi

    if [ -n "$installed" ]; then
        echo "  Installed: $installed — upgrading..."
    else
        echo "  No existing installation found — installing..."
    fi

    TMP_DIR="$(mktemp -d)"
    trap 'rm -rf "$TMP_DIR"' EXIT

    echo "Downloading db v${version}..."
    gh release download "v${version}" -R "$REPO" -p "$asset" -D "$TMP_DIR"

    echo "Extracting..."
    unzip -qo "${TMP_DIR}/${asset}" -d "${TMP_DIR}/extracted"

    local binary_path="${TMP_DIR}/extracted/DatabaseMcp.Cli"
    if [ ! -f "$binary_path" ]; then
        echo "Binary not found in archive." >&2
        exit 1
    fi

    chmod +x "$binary_path"

    echo "Installing to ${INSTALL_DIR}/${BINARY_NAME}..."
    if [ -w "$INSTALL_DIR" ]; then
        mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
    else
        sudo mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
    fi

    echo "Installed db $version successfully."
}

main

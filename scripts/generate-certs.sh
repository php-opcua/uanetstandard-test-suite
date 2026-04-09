#!/bin/sh
set -e

CERTS_DIR="/certs"
CA_DIR="$CERTS_DIR/ca"
SERVER_DIR="$CERTS_DIR/server"
CLIENT_DIR="$CERTS_DIR/client"
TRUSTED_DIR="$CERTS_DIR/trusted"
REJECTED_DIR="$CERTS_DIR/rejected"
SELF_SIGNED_DIR="$CERTS_DIR/self-signed"
EXPIRED_DIR="$CERTS_DIR/expired"
PKI_DIR="$CERTS_DIR/pki"

# Skip if already generated
if [ -f "$CA_DIR/ca-cert.pem" ] && [ -f "$SERVER_DIR/cert.pem" ] && [ -f "$CLIENT_DIR/cert.pem" ]; then
    echo "Certificates already exist, skipping generation."
    exit 0
fi

echo "=== Generating OPC UA Test Certificates ==="

apk add --no-cache openssl > /dev/null 2>&1 || true

# Create directories
for dir in "$CA_DIR" "$SERVER_DIR" "$CLIENT_DIR" "$TRUSTED_DIR" "$REJECTED_DIR" \
           "$SELF_SIGNED_DIR" "$EXPIRED_DIR" \
           "$PKI_DIR/trusted/certs" "$PKI_DIR/trusted/crl" \
           "$PKI_DIR/issuers/certs" "$PKI_DIR/issuers/crl" \
           "$PKI_DIR/rejected/certs"; do
    mkdir -p "$dir"
done

# ============================================
# 1. Certificate Authority (CA)
# ============================================
echo "--- Generating CA certificate ---"

openssl genrsa -out "$CA_DIR/ca-key.pem" 4096

openssl req -new -x509 -days 3650 -key "$CA_DIR/ca-key.pem" \
    -out "$CA_DIR/ca-cert.pem" \
    -subj "/C=US/ST=Test/L=TestCity/O=OPC UA Test Suite/OU=CA/CN=OPC UA Test CA"

# Convert CA cert to DER
openssl x509 -in "$CA_DIR/ca-cert.pem" -outform DER -out "$CA_DIR/ca-cert.der"

# Create a simple empty CRL
touch /tmp/ca-index.txt
echo "01" > /tmp/ca-crlnumber

cat > /tmp/ca-openssl.cnf << 'CNFEOF'
[ca]
default_ca = CA_default
[CA_default]
database = /tmp/ca-index.txt
crlnumber = /tmp/ca-crlnumber
default_crl_days = 3650
default_md = sha256
CNFEOF

openssl ca -gencrl -keyfile "$CA_DIR/ca-key.pem" -cert "$CA_DIR/ca-cert.pem" \
    -out "$CA_DIR/ca-crl.pem" -config /tmp/ca-openssl.cnf 2>/dev/null || touch "$CA_DIR/ca-crl.pem"

# ============================================
# 2. Server Certificate
# ============================================
echo "--- Generating Server certificate ---"

cat > /tmp/server-ext.cnf << 'EXTEOF'
[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment, dataEncipherment, nonRepudiation
extendedKeyUsage = serverAuth, clientAuth
subjectAltName = @alt_names

[alt_names]
URI.1 = urn:opcua:testserver:nodes
DNS.1 = localhost
DNS.2 = opcua-no-security
DNS.3 = opcua-userpass
DNS.4 = opcua-certificate
DNS.5 = opcua-all-security
DNS.6 = opcua-discovery
DNS.7 = opcua-auto-accept
DNS.8 = opcua-sign-only
DNS.9 = opcua-legacy
DNS.10 = opcua-ecc-nist
DNS.11 = opcua-ecc-brainpool
IP.1 = 127.0.0.1
IP.2 = 0.0.0.0
EXTEOF

openssl genrsa -out "$SERVER_DIR/key.pem" 2048

openssl req -new -key "$SERVER_DIR/key.pem" -out /tmp/server.csr \
    -subj "/C=US/ST=Test/L=TestCity/O=OPC UA Test Suite/OU=Server/CN=OPC UA Test Server"

openssl x509 -req -days 3650 -in /tmp/server.csr \
    -CA "$CA_DIR/ca-cert.pem" -CAkey "$CA_DIR/ca-key.pem" -CAcreateserial \
    -out "$SERVER_DIR/cert.pem" \
    -extfile /tmp/server-ext.cnf -extensions v3_req

# Convert to DER
openssl x509 -in "$SERVER_DIR/cert.pem" -outform DER -out "$SERVER_DIR/cert.der"
openssl rsa -in "$SERVER_DIR/key.pem" -outform DER -out "$SERVER_DIR/key.der"

# Create PFX (for .NET) - empty password
openssl pkcs12 -export -out "$SERVER_DIR/server.pfx" \
    -inkey "$SERVER_DIR/key.pem" -in "$SERVER_DIR/cert.pem" \
    -certfile "$CA_DIR/ca-cert.pem" -passout pass:

# ============================================
# 3. Client Certificate
# ============================================
echo "--- Generating Client certificate ---"

cat > /tmp/client-ext.cnf << 'EXTEOF'
[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment, dataEncipherment, nonRepudiation
extendedKeyUsage = clientAuth
subjectAltName = @alt_names

[alt_names]
URI.1 = urn:opcua:testclient
EXTEOF

openssl genrsa -out "$CLIENT_DIR/key.pem" 2048

openssl req -new -key "$CLIENT_DIR/key.pem" -out /tmp/client.csr \
    -subj "/C=US/ST=Test/L=TestCity/O=OPC UA Test Suite/OU=Client/CN=OPC UA Test Client"

openssl x509 -req -days 3650 -in /tmp/client.csr \
    -CA "$CA_DIR/ca-cert.pem" -CAkey "$CA_DIR/ca-key.pem" -CAcreateserial \
    -out "$CLIENT_DIR/cert.pem" \
    -extfile /tmp/client-ext.cnf -extensions v3_req

# Convert to DER
openssl x509 -in "$CLIENT_DIR/cert.pem" -outform DER -out "$CLIENT_DIR/cert.der"
openssl rsa -in "$CLIENT_DIR/key.pem" -outform DER -out "$CLIENT_DIR/key.der"

# Create PFX (for .NET)
openssl pkcs12 -export -out "$CLIENT_DIR/client.pfx" \
    -inkey "$CLIENT_DIR/key.pem" -in "$CLIENT_DIR/cert.pem" \
    -certfile "$CA_DIR/ca-cert.pem" -passout pass:

# ============================================
# 4. Self-Signed Certificate (for rejection testing)
# ============================================
echo "--- Generating Self-Signed certificate ---"

openssl genrsa -out "$SELF_SIGNED_DIR/key.pem" 2048

openssl req -new -x509 -days 3650 -key "$SELF_SIGNED_DIR/key.pem" \
    -out "$SELF_SIGNED_DIR/cert.pem" \
    -subj "/C=US/ST=Test/L=TestCity/O=Untrusted/CN=Self Signed Client"

openssl x509 -in "$SELF_SIGNED_DIR/cert.pem" -outform DER -out "$SELF_SIGNED_DIR/cert.der"
openssl rsa -in "$SELF_SIGNED_DIR/key.pem" -outform DER -out "$SELF_SIGNED_DIR/key.der"

# ============================================
# 5. Expired Certificate (for expiration testing)
# ============================================
echo "--- Generating Expired certificate ---"

openssl genrsa -out "$EXPIRED_DIR/key.pem" 2048

openssl req -new -key "$EXPIRED_DIR/key.pem" -out /tmp/expired.csr \
    -subj "/C=US/ST=Test/L=TestCity/O=OPC UA Test Suite/OU=Expired/CN=Expired Client"

# Create cert valid for only 1 day (will be almost expired)
openssl x509 -req -days 1 -in /tmp/expired.csr \
    -CA "$CA_DIR/ca-cert.pem" -CAkey "$CA_DIR/ca-key.pem" -CAcreateserial \
    -out "$EXPIRED_DIR/cert.pem"

openssl x509 -in "$EXPIRED_DIR/cert.pem" -outform DER -out "$EXPIRED_DIR/cert.der"

# ============================================
# 6. Setup PKI directories
# ============================================
echo "--- Setting up PKI directories ---"

# Trust the client certificate
cp "$CLIENT_DIR/cert.pem" "$TRUSTED_DIR/"
cp "$CLIENT_DIR/cert.der" "$TRUSTED_DIR/"

# PKI structure for UA-.NETStandard
cp "$CLIENT_DIR/cert.der" "$PKI_DIR/trusted/certs/"
cp "$CA_DIR/ca-cert.der" "$PKI_DIR/issuers/certs/"
cp "$CA_DIR/ca-crl.pem" "$PKI_DIR/issuers/crl/" 2>/dev/null || true

# ============================================
# 7. Set permissions
# ============================================
echo "--- Setting permissions ---"
chmod -R 755 "$CERTS_DIR"
chmod 644 "$SERVER_DIR/key.pem" "$CLIENT_DIR/key.pem" "$CA_DIR/ca-key.pem"

echo ""
echo "=== Certificate generation complete ==="
echo "CA Certificate:     $CA_DIR/ca-cert.pem"
echo "Server Certificate: $SERVER_DIR/cert.pem"
echo "Server PFX:         $SERVER_DIR/server.pfx"
echo "Client Certificate: $CLIENT_DIR/cert.pem"
echo "Client PFX:         $CLIENT_DIR/client.pfx"
echo "Self-Signed:        $SELF_SIGNED_DIR/cert.pem"
echo "Expired:            $EXPIRED_DIR/cert.pem"

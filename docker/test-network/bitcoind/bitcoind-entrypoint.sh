#!/bin/bash
set -Eeuo pipefail

echo Starting bitcoind...
bitcoind -datadir=/bitcoind -daemon
# until bitcoin-cli -datadir=/bitcoind getblockchaininfo  > /dev/null 2>&1
# do
# 	sleep 1
# done
# echo bitcoind started
# export address=`cat /bitcoind/keys/demo_address.txt`
# export privkey=`cat /bitcoind/keys/demo_privkey.txt`
# echo "================================================"
# echo "Importing demo private key"
# echo "Bitcoin address: " ${address}
# echo "Private key: " ${privkey}
# echo "================================================"
# bitcoin-cli -datadir=/bitcoind createwallet regtest 
# bitcoin-cli -datadir=/bitcoind importprivkey $privkey

# Executing CMD
echo "$@"
exec "$@"

# Show LND log from beginning and follow
tail -n +1 -f /bitcoind/regtest/debug.log || true
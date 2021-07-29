#!/bin/bash
set -Eeuo pipefail

docker exec bitcoind bitcoin-cli -datadir=/bitcoind -rpcwallet=regtest -generate 100

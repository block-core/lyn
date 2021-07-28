#!/bin/bash
set -Eeuo pipefail

docker exec bitcoind bitcoin-cli -datadir=/bitcoind createwallet regtest

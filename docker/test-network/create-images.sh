
REGISTRY=docker.com
NAME=lyn


docker build -t lyn/bitcoind bitcoind
docker build -t lyn/clightning clightning
docker build -t lyn/lnd lnd
docker build -t lyn/eclair eclair


# docker push lyn/bitcoind:latest
# docker push lyn/clightning:latest
# docker push lyn/lnd:latest
# docker push lyn/eclair:latest


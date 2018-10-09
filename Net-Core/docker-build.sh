#!/bin/bash

set -e
OPTIONS=$(getopt -o '' -l artifactory-user:,artifactory-password:,artifactory-uri:,version-file:,clean -- "$@")

eval set -- "${OPTIONS}"

while [ $# -gt 0 ]
do
  case $1 in
    --artifactory-user) ARTIFACTORY_USER=$2; shift;;
    --artifactory-password) ARTIFACTORY_PASSWORD=$2; shift;;
    --artifactory-uri) ARTIFACTORY_URI=$2; shift;;
    --version-file) VERSION_FILE=$2; shift;;
    --clean) CLEAN=$true;;
    (--) shift; break;;
    (-*) echo "$0: error - unrecognized option $1" 1>&2; exit 1;;
    (*) break;;
  esac
  shift
done

if [ "x$ARTIFACTORY_URI" != "x" ] && ( [ "x$ARTIFACTORY_USER" == "x" ] || [ "x$ARTIFACTORY_PASSWORD" == "x" ] ) then
  echo "Usage: ./docker-build.sh [--artifactory-uri <uri> --artifactory-user <user> --artifactory-password <password>] [--version-file <file>] [--clean]" && exit 1
fi

if [ "x$VERSION_FILE" != "x" ] && [ -f $VERSION_FILE ]; then
  VERSION=$(cat $VERSION_FILE)
else
  VERSION=$(gitversion /showvariable FullSemVer | sed s/\+/\-/)
fi

IMAGE_NAME=YOUR_IMAGE_NAME
IMAGE_PATH=LIKE_YOUR_PRODUCT_NAME
IMAGE=$IMAGE_PATH/$IMAGE_NAME

if [ "x$ARTIFACTORY_URI" != "x" ]; then
  IMAGE=$ARTIFACTORY_URI/$IMAGE
fi

echo "IMAGE: $IMAGE:$VERSION"

TAG=$IMAGE:$VERSION
echo "TAG: $TAG"

docker build --no-cache --tag $TAG --build-arg source=publish/ --file Dockerfile .

if [ "x$ARTIFACTORY_URI" != "x" ]; then
  docker login --username $ARTIFACTORY_USER --password $ARTIFACTORY_PASSWORD $ARTIFACTORY_URI
  docker push $TAG
  # docker tag $TAG $IMAGE:on-build
  # docker push $IMAGE:on-build
  if [ "$CLEAN" = true ]; then
     docker image rm --force $(docker image inspect --format '{{ .ID }}' $TAG)
  fi
elif [ "$CLEAN" = true ]; then
  docker image rm $TAG
fi

if [ "x$VERSION_FILE" != "x" ]; then
  echo $VERSION > $VERSION_FILE
fi
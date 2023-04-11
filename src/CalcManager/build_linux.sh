#!/bin/bash

buildManager() {
	CALCMANAGER_PATH=bin/runtimes/linux-$2/native
	mkdir -p $CALCMANAGER_PATH

	echo "Building CalcManager for $2 - $CALCMANAGER_PATH"

	$1 \
		-std=c++1z \
		-D__LINUX__=1 \
		-fPIC \
		-shared \
		-static-libstdc++ \
		-Bstatic\
		-lgcc \
		-lstdc++ \
		-o $CALCMANAGER_PATH/libCalcManager.so \
		CEngine/*.cpp Ratpack/*.cpp *.cpp -I.
}

buildManager "g++" "x64"
buildManager "arm-linux-gnueabihf-g++" "arm"
buildManager "aarch64-linux-gnu-g++" "arm64"

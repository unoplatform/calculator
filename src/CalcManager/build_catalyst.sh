#!/bin/sh -e

build_libCalc()
{
	BUILD_ARCH=$1
	BUILD_TARGET=$2
	BUILD_SDK=$3
	BUILD_CFLAGS=$4

    rm *.o || true

    echo "Building for $1"

    xcrun -sdk ${BUILD_SDK} clang \
        -x c++ \
        -arch ${BUILD_ARCH} \
        ${BUILD_CFLAGS} \
        -std=c++1z \
        -stdlib=libc++ \
        -target ${BUILD_TARGET} \
        -c \
        CEngine/*.cpp RatPack/*.cpp *.cpp -I.

    mkdir -p bin/${BUILD_ARCH}
    mkdir -p ../Calculator.Mobile/MacCatalyst/NativeReferences/${BUILD_ARCH}

    libtool \
        -static \
        *.o \
        -o ../Calculator.Mobile/MacCatalyst/NativeReferences/${BUILD_ARCH}/libCalcManager.a

    rm *.o || true
}

build_libCalc x86_64 "x86_64-apple-ios14.0-macabi" macosx "-miphoneos-version-min=13.0"
build_libCalc arm64 "arm64-apple-ios14.0-macabi" macosx "-miphoneos-version-min=13.0"
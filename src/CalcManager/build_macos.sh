#!/bin/bash

xcrun -sdk macosx clang \
    -x c++ \
    -std=c++1z \
    -stdlib=libc++ \
    -c \
	CEngine/*.cpp RatPack/*.cpp *.cpp -I.  

mkdir bin

clang \
    -x c++ \
    -arch x86_64 \
    -std=c++1z \
    -stdlib=libc++ \
    -c \
	CEngine/*.cpp RatPack/*.cpp *.cpp -I.  

mkdir bin/x86_64

libtool \
    -static \
    *.o \
    -o ../Calculator.Mobile/macOS/NativeReferences/x86_64/libCalcManager.a

rm *.o


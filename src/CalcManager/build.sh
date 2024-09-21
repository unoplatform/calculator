# To run this script locally:
# - if cloned from windows, ensure that line endings are `LF`
# - under linux: cd to your_path\calculator\src\CalcManager
# - under WSL: cd `wslpath "your_path\calculator\src\CalcManager"`
# Execute the following:
#   git clone --branch 3.1.34 https://github.com/emscripten-core/emsdk
#   ./emsdk install 3.1.34
#   ./emsdk activate 3.1.34
#   source ./emsdk_env.sh
# Navigate back to this file's folder in the same terminal

mkdir -p bin/wasm/CalcManager.a/3.0.0/st,simd

echo Generating LLVM Bitcode files
emcc \
	-std=c++17 \
	-s WASM=1 \
	-s LEGALIZE_JS_FFI=0 \
	-s RESERVED_FUNCTION_POINTERS=64 \
    -s ALLOW_MEMORY_GROWTH=1 \
	-s BINARYEN=1 \
	-fwasm-exceptions \
	-o bin/wasm/CalcManager.a/3.0.0/st,simd/CalcManager.a \
	-r \
	CEngine/*.cpp Ratpack/*.cpp *.cpp -I.

cmake_minimum_required(VERSION 3.24)
project(fastfsm_cpp_bench LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)

# Google Benchmark z vcpkg
find_package(benchmark CONFIG REQUIRED)

add_executable(fastfsm_cpp_bench main.cpp)
target_link_libraries(fastfsm_cpp_bench
    PRIVATE benchmark::benchmark benchmark::benchmark_main Shlwapi)

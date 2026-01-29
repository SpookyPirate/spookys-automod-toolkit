cmake_minimum_required(VERSION 3.24)

# ========================================
# Project Configuration
# ========================================
project(
    {{PROJECT_NAME}}
    VERSION {{VERSION_MAJOR}}.{{VERSION_MINOR}}.{{VERSION_PATCH}}
    LANGUAGES CXX
)

# ========================================
# Build Options
# ========================================
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_INTERPROCEDURAL_OPTIMIZATION ON)
set(CMAKE_INTERPROCEDURAL_OPTIMIZATION_DEBUG OFF)

# MSVC-specific flags
if(MSVC)
    add_compile_options(
        /MP     # Multi-processor compilation
        /W4     # Warning level 4
        /WX     # Treat warnings as errors
        /permissive-    # Standards conformance
        /Zc:preprocessor    # Use conforming preprocessor
        /EHsc   # Exception handling
        /utf-8  # UTF-8 source files

        # Disable specific warnings
        /wd4068 # unknown pragma 'clang'
        /wd4189 # local variable is initialized but not referenced
        /wd4200 # nonstandard extension used : zero-sized array in struct/union
        /wd4201 # nonstandard extension used : nameless struct/union
        /wd4324 # structure was padded due to alignment specifier
    )
endif()

# ========================================
# Dependencies
# ========================================

# Option 1: Use VCPKG (recommended)
# Uncomment these lines if you have VCPKG set up:
find_package(CommonLibSSE CONFIG REQUIRED)
# find_package(spdlog CONFIG REQUIRED)
# find_package(xbyak CONFIG REQUIRED)

# Option 2: Manual vendor/ folder (for users without global VCPKG)
# Download and extract the following into vendor/:
# - CommonLibSSE-NG: https://github.com/CharmedBaryon/CommonLibSSE-NG
# - spdlog: https://github.com/gabime/spdlog
# - xbyak: https://github.com/herumi/xbyak

# Uncomment these lines for manual vendor setup:
# set(CommonLibSSE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/vendor/CommonLibSSE-NG")
# set(spdlog_DIR "${CMAKE_CURRENT_SOURCE_DIR}/vendor/spdlog")
# set(xbyak_DIR "${CMAKE_CURRENT_SOURCE_DIR}/vendor/xbyak")
#
# add_subdirectory("${CommonLibSSE_DIR}" CommonLibSSE EXCLUDE_FROM_ALL)
# add_subdirectory("${spdlog_DIR}" spdlog EXCLUDE_FROM_ALL)
#
# # Xbyak is header-only, just add include directory
# add_library(xbyak INTERFACE)
# target_include_directories(xbyak INTERFACE "${xbyak_DIR}")

# ========================================
# Source Files
# ========================================
set(SOURCES
    src/main.cpp
    src/PCH.h
)

# ========================================
# Build Target
# ========================================
add_library(${PROJECT_NAME} SHARED ${SOURCES})

target_include_directories(${PROJECT_NAME}
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/src
)

target_link_libraries(${PROJECT_NAME}
    PRIVATE
        CommonLibSSE::CommonLibSSE
)

# ========================================
# Precompiled Headers
# ========================================
target_precompile_headers(${PROJECT_NAME}
    PRIVATE
        src/PCH.h
)

# ========================================
# Output Configuration
# ========================================
set_target_properties(${PROJECT_NAME}
    PROPERTIES
        OUTPUT_NAME "${PROJECT_NAME}"
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
)

# ========================================
# Post-Build: Copy to Skyrim Data/SKSE/Plugins
# ========================================
# Uncomment and set your Skyrim path to auto-deploy after build:
# set(SKYRIM_PATH "C:/Program Files (x86)/Steam/steamapps/common/Skyrim Special Edition")
# add_custom_command(
#     TARGET ${PROJECT_NAME} POST_BUILD
#     COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:${PROJECT_NAME}> "${SKYRIM_PATH}/Data/SKSE/Plugins/"
#     COMMAND ${CMAKE_COMMAND} -E echo "Deployed to: ${SKYRIM_PATH}/Data/SKSE/Plugins/"
# )

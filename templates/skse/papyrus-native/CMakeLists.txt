cmake_minimum_required(VERSION 3.21)

project(
    {{PROJECT_NAME}}
    VERSION {{VERSION_MAJOR}}.{{VERSION_MINOR}}.{{VERSION_PATCH}}
    LANGUAGES CXX
)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_INTERPROCEDURAL_OPTIMIZATION ON)

# CommonLibSSE-NG
find_package(CommonLibSSE CONFIG REQUIRED)

# Plugin configuration
set(PLUGIN_NAME "${PROJECT_NAME}")
set(PLUGIN_AUTHOR "{{AUTHOR}}")
set(PLUGIN_VERSION "${PROJECT_VERSION}")

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/cmake/version.rc.in
    ${CMAKE_CURRENT_BINARY_DIR}/version.rc
    @ONLY
)

# Source files
set(SOURCES
    src/main.cpp
    src/plugin.cpp
    src/papyrus.cpp
)

set(HEADERS
    src/plugin.h
    src/papyrus.h
)

# Create DLL
add_library(${PROJECT_NAME} SHARED
    ${SOURCES}
    ${HEADERS}
    ${CMAKE_CURRENT_BINARY_DIR}/version.rc
)

target_compile_definitions(${PROJECT_NAME} PRIVATE
    PLUGIN_NAME="${PLUGIN_NAME}"
    PLUGIN_AUTHOR="${PLUGIN_AUTHOR}"
    PLUGIN_VERSION="${PLUGIN_VERSION}"
)

target_include_directories(${PROJECT_NAME} PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

target_link_libraries(${PROJECT_NAME} PRIVATE
    CommonLibSSE::CommonLibSSE
)

# Output directory
set_target_properties(${PROJECT_NAME} PROPERTIES
    OUTPUT_NAME "${PLUGIN_NAME}"
    SUFFIX ".dll"
)

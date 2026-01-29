#pragma once

// ========================================
// Precompiled Header for {{PROJECT_NAME}}
// ========================================
// This PCH includes CommonLibSSE-NG headers.
// IMPORTANT: This file is used by CMake target_precompile_headers()
// DO NOT manually include this in your .cpp files - CMake handles it automatically

// Standard Library
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <string>
#include <string_view>
#include <vector>
#include <unordered_map>
#include <memory>
#include <functional>
#include <algorithm>
#include <ranges>

// C++20 features
using namespace std::literals;

// CommonLibSSE-NG (automatically includes all RE:: and SKSE:: headers)
#include <RE/Skyrim.h>
#include <SKSE/SKSE.h>

// Version information
#define MAKE_STR_HELPER(x) #x
#define MAKE_STR(x) MAKE_STR_HELPER(x)

#define PLUGIN_NAME "{{PROJECT_NAME}}"
#define PLUGIN_AUTHOR "{{AUTHOR}}"
#define PLUGIN_VERSION MAKE_STR({{VERSION_MAJOR}}) "." MAKE_STR({{VERSION_MINOR}}) "." MAKE_STR({{VERSION_PATCH}})

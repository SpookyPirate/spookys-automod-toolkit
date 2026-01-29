// ========================================
// {{PROJECT_NAME}} - SKSE Plugin
// Author: {{AUTHOR}}
// Version: {{VERSION_MAJOR}}.{{VERSION_MINOR}}.{{VERSION_PATCH}}
// Description: {{DESCRIPTION}}
// ========================================

#include "PCH.h"

// ========================================
// Plugin Metadata (C++20 Syntax)
// ========================================
// IMPORTANT: Use C++20 designated initializers, NOT the old CompatibleVersions array
constexpr SKSE::PluginInfo PluginInfo{
    .Version = { {{VERSION_MAJOR}}, {{VERSION_MINOR}}, {{VERSION_PATCH}}, 0 },
    .Name = PLUGIN_NAME##sv,
    .Author = PLUGIN_AUTHOR##sv,
    .SupportEmail = {}
};

extern "C" __declspec(dllexport) constinit SKSE::PluginVersionData SKSEPlugin_Version = [] {
    SKSE::PluginVersionData data{};
    data.PluginVersion(PluginInfo.Version);
    data.PluginName(PluginInfo.Name);
    data.AuthorName(PluginInfo.Author);
    data.UsesAddressLibrary(true);
    data.UsesSigScanning(false);
    data.IsLayoutDependent(false);
    data.HasNoStructUse(false);

    // Compatible with all runtime versions (SE, AE, VR, etc.)
    data.RuntimeCompatibility(SKSE::RuntimeCompatibility::Independent);

    return data;
}();

// ========================================
// Event Sinks
// ========================================

// Example: OnHit Event Handler
class OnHitEventHandler : public RE::BSTEventSink<RE::TESHitEvent>
{
public:
    static OnHitEventHandler* GetSingleton()
    {
        static OnHitEventHandler singleton;
        return &singleton;
    }

    RE::BSEventNotifyControl ProcessEvent(const RE::TESHitEvent* event, RE::BSTEventSource<RE::TESHitEvent>*)
    {
        if (!event) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Get the target that was hit
        auto target = event->target.get();
        if (!target) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Check if target is an actor (safe casting)
        auto targetActor = target->As<RE::Actor>();
        if (!targetActor) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Get the aggressor (who hit the target)
        auto aggressor = event->cause.get();
        if (!aggressor) {
            return RE::BSEventNotifyControl::kContinue;
        }

        auto aggressorActor = aggressor->As<RE::Actor>();
        if (!aggressorActor) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Example: Log the hit event
        SKSE::log::info(
            "OnHit: {} hit {} for {} damage",
            aggressorActor->GetName(),
            targetActor->GetName(),
            event->damageAmount
        );

        // Example: Access actor properties safely
        if (targetActor->IsDead()) {
            SKSE::log::info("{} is dead", targetActor->GetName());
        }

        return RE::BSEventNotifyControl::kContinue;
    }

private:
    OnHitEventHandler() = default;
    OnHitEventHandler(const OnHitEventHandler&) = delete;
    OnHitEventHandler(OnHitEventHandler&&) = delete;
    ~OnHitEventHandler() = default;

    OnHitEventHandler& operator=(const OnHitEventHandler&) = delete;
    OnHitEventHandler& operator=(OnHitEventHandler&&) = delete;
};

// Example: OnEquip Event Handler
class OnEquipEventHandler : public RE::BSTEventSink<RE::TESEquipEvent>
{
public:
    static OnEquipEventHandler* GetSingleton()
    {
        static OnEquipEventHandler singleton;
        return &singleton;
    }

    RE::BSEventNotifyControl ProcessEvent(const RE::TESEquipEvent* event, RE::BSTEventSource<RE::TESEquipEvent>*)
    {
        if (!event) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Get the actor who equipped/unequipped
        auto actor = RE::TESForm::LookupByID<RE::Actor>(event->actor);
        if (!actor) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Get the equipped item
        auto item = RE::TESForm::LookupByID(event->baseObject);
        if (!item) {
            return RE::BSEventNotifyControl::kContinue;
        }

        // Log equip/unequip
        SKSE::log::info(
            "{} {} {}",
            actor->GetName(),
            event->equipped ? "equipped" : "unequipped",
            item->GetName()
        );

        return RE::BSEventNotifyControl::kContinue;
    }

private:
    OnEquipEventHandler() = default;
    OnEquipEventHandler(const OnEquipEventHandler&) = delete;
    OnEquipEventHandler(OnEquipEventHandler&&) = delete;
    ~OnEquipEventHandler() = default;

    OnEquipEventHandler& operator=(const OnEquipEventHandler&) = delete;
    OnEquipEventHandler& operator=(OnEquipEventHandler&&) = delete;
};

// ========================================
// Helper Functions
// ========================================

// Example: Safe form lookup by EditorID
RE::TESForm* LookupFormByEditorID(std::string_view editorID)
{
    auto form = RE::TESForm::LookupByEditorID(editorID);
    if (!form) {
        SKSE::log::warn("Form not found: {}", editorID);
    }
    return form;
}

// Example: Safe form lookup by FormID
RE::TESForm* LookupFormByID(RE::FormID formID)
{
    auto form = RE::TESForm::LookupByID(formID);
    if (!form) {
        SKSE::log::warn("Form not found: {:08X}", formID);
    }
    return form;
}

// Example: Get player actor safely
RE::Actor* GetPlayer()
{
    auto player = RE::PlayerCharacter::GetSingleton();
    if (!player) {
        SKSE::log::error("Failed to get player character");
    }
    return player;
}

// Example: Add item to player inventory
bool AddItemToPlayer(RE::TESBoundObject* item, uint32_t count = 1)
{
    auto player = GetPlayer();
    if (!player || !item) {
        return false;
    }

    player->AddObjectToContainer(item, nullptr, count, nullptr);
    SKSE::log::info("Added {} x{} to player inventory", item->GetName(), count);
    return true;
}

// ========================================
// Plugin Initialization
// ========================================

void InitializeEventHandlers()
{
    // Register OnHit event handler
    auto scriptEventSource = RE::ScriptEventSourceHolder::GetSingleton();
    if (scriptEventSource) {
        scriptEventSource->AddEventSink<RE::TESHitEvent>(OnHitEventHandler::GetSingleton());
        SKSE::log::info("Registered OnHit event handler");

        scriptEventSource->AddEventSink<RE::TESEquipEvent>(OnEquipEventHandler::GetSingleton());
        SKSE::log::info("Registered OnEquip event handler");
    } else {
        SKSE::log::error("Failed to get ScriptEventSourceHolder");
    }
}

void MessageHandler(SKSE::MessagingInterface::Message* message)
{
    switch (message->type) {
        case SKSE::MessagingInterface::kDataLoaded:
            SKSE::log::info("Data loaded event received");
            InitializeEventHandlers();
            break;

        case SKSE::MessagingInterface::kPostLoad:
            SKSE::log::info("Post load event received");
            break;

        case SKSE::MessagingInterface::kPreLoadGame:
            SKSE::log::info("Pre load game event received");
            break;

        case SKSE::MessagingInterface::kPostLoadGame:
            SKSE::log::info("Post load game event received");
            break;

        case SKSE::MessagingInterface::kNewGame:
            SKSE::log::info("New game event received");
            break;

        case SKSE::MessagingInterface::kPostPostLoad:
            SKSE::log::info("Post post load event received");
            break;
    }
}

// ========================================
// Entry Point
// ========================================

extern "C" __declspec(dllexport) bool SKSEAPI SKSEPlugin_Load(const SKSE::LoadInterface* skse)
{
    // Initialize logging
    SKSE::Init(skse, false);

    SKSE::log::info("========================================");
    SKSE::log::info("{} v{}", PLUGIN_NAME, PLUGIN_VERSION);
    SKSE::log::info("Author: {}", PLUGIN_AUTHOR);
    SKSE::log::info("========================================");

    // Register message handler
    auto messaging = SKSE::GetMessagingInterface();
    if (!messaging || !messaging->RegisterListener(MessageHandler)) {
        SKSE::log::error("Failed to register message listener");
        return false;
    }

    SKSE::log::info("Plugin loaded successfully");
    return true;
}

extern "C" __declspec(dllexport) constinit auto SKSEPlugin_Query = [](const SKSE::QueryInterface*) {
    // This function is deprecated in CommonLibSSE-NG
    // All plugin info is now provided via SKSEPlugin_Version
    return true;
};

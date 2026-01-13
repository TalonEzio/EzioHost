export function onSettingsSaved() {
    // Show success animation or notification
    console.log("Settings saved successfully");

    // Optional: Add visual feedback
    if (typeof window !== "undefined" && window.dispatchEvent) {
        window.dispatchEvent(new CustomEvent("settingsSaved"));
    }
}
export function initSettingsPage() {
    console.log('Settings page initialized');
    
    // Optional: Add any page-specific JavaScript functionality here
    if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('settingsPageLoaded'));
    }
}

import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import en from "@/locales/en.json";
import ru from "@/locales/ru.json";

const languageStorageKey = "aureus.language";
const storedLanguage = localStorage.getItem(languageStorageKey);
const defaultLanguage = storedLanguage === "en" || storedLanguage === "ru" ? storedLanguage : "ru";

void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    ru: { translation: ru },
  },
  lng: defaultLanguage,
  fallbackLng: "ru",
  interpolation: {
    escapeValue: false,
  },
});

i18n.on("languageChanged", (language) => {
  if (language === "en" || language === "ru") {
    localStorage.setItem(languageStorageKey, language);
    document.documentElement.lang = language;
  }
});

document.documentElement.lang = defaultLanguage;

export default i18n;

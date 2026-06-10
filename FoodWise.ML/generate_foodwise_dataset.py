# FoodWise için sentetik gıda israf riski veri seti üretir.
# Bu veri seti, ML modeliyle ürünlerin Low / Medium / High / Critical risk seviyesini tahmin etmek için kullanılacaktır.

import random
import pandas as pd


products = [
    # Süt Ürünleri
    {"name": "Süt", "category": "Süt Ürünleri", "sensitive": True, "base_life": 7},
    {"name": "Yoğurt", "category": "Süt Ürünleri", "sensitive": True, "base_life": 10},
    {"name": "Peynir", "category": "Süt Ürünleri", "sensitive": True, "base_life": 20},
    {"name": "Kaşar Peyniri", "category": "Süt Ürünleri", "sensitive": True, "base_life": 25},
    {"name": "Ayran", "category": "Süt Ürünleri", "sensitive": True, "base_life": 5},
    {"name": "Krema", "category": "Süt Ürünleri", "sensitive": True, "base_life": 6},

    # Et / Balık / Tavuk
    {"name": "Tavuk", "category": "Et Ürünleri", "sensitive": True, "base_life": 4},
    {"name": "Kıyma", "category": "Et Ürünleri", "sensitive": True, "base_life": 3},
    {"name": "Kırmızı Et", "category": "Et Ürünleri", "sensitive": True, "base_life": 5},
    {"name": "Balık", "category": "Et Ürünleri", "sensitive": True, "base_life": 3},
    {"name": "Sucuk", "category": "Et Ürünleri", "sensitive": True, "base_life": 20},
    {"name": "Salam", "category": "Et Ürünleri", "sensitive": True, "base_life": 10},

    # Sebzeler
    {"name": "Domates", "category": "Sebze", "sensitive": True, "base_life": 7},
    {"name": "Salatalık", "category": "Sebze", "sensitive": True, "base_life": 6},
    {"name": "Marul", "category": "Sebze", "sensitive": True, "base_life": 5},
    {"name": "Biber", "category": "Sebze", "sensitive": True, "base_life": 7},
    {"name": "Havuç", "category": "Sebze", "sensitive": False, "base_life": 18},
    {"name": "Patates", "category": "Sebze", "sensitive": False, "base_life": 30},
    {"name": "Soğan", "category": "Sebze", "sensitive": False, "base_life": 35},
    {"name": "Kabak", "category": "Sebze", "sensitive": True, "base_life": 8},
    {"name": "Ispanak", "category": "Sebze", "sensitive": True, "base_life": 5},
    {"name": "Brokoli", "category": "Sebze", "sensitive": True, "base_life": 6},

    # Meyveler
    {"name": "Muz", "category": "Meyve", "sensitive": True, "base_life": 6},
    {"name": "Elma", "category": "Meyve", "sensitive": False, "base_life": 20},
    {"name": "Armut", "category": "Meyve", "sensitive": True, "base_life": 10},
    {"name": "Çilek", "category": "Meyve", "sensitive": True, "base_life": 4},
    {"name": "Üzüm", "category": "Meyve", "sensitive": True, "base_life": 7},
    {"name": "Portakal", "category": "Meyve", "sensitive": False, "base_life": 18},
    {"name": "Limon", "category": "Meyve", "sensitive": False, "base_life": 25},
    {"name": "Karpuz", "category": "Meyve", "sensitive": True, "base_life": 7},

    # Fırın Ürünleri
    {"name": "Ekmek", "category": "Fırın Ürünü", "sensitive": False, "base_life": 4},
    {"name": "Lavaş", "category": "Fırın Ürünü", "sensitive": False, "base_life": 5},
    {"name": "Tost Ekmeği", "category": "Fırın Ürünü", "sensitive": False, "base_life": 7},
    {"name": "Poğaça", "category": "Fırın Ürünü", "sensitive": True, "base_life": 3},
    {"name": "Simit", "category": "Fırın Ürünü", "sensitive": False, "base_life": 2},

    # Kuru Gıdalar
    {"name": "Pirinç", "category": "Kuru Gıda", "sensitive": False, "base_life": 180},
    {"name": "Makarna", "category": "Kuru Gıda", "sensitive": False, "base_life": 240},
    {"name": "Mercimek", "category": "Kuru Gıda", "sensitive": False, "base_life": 180},
    {"name": "Nohut", "category": "Kuru Gıda", "sensitive": False, "base_life": 200},
    {"name": "Bulgur", "category": "Kuru Gıda", "sensitive": False, "base_life": 180},
    {"name": "Un", "category": "Kuru Gıda", "sensitive": False, "base_life": 120},
    {"name": "Şeker", "category": "Kuru Gıda", "sensitive": False, "base_life": 365},

    # Pişmiş Yemekler
    {"name": "Pişmiş Pilav", "category": "Pişmiş Yemek", "sensitive": True, "base_life": 3},
    {"name": "Pişmiş Makarna", "category": "Pişmiş Yemek", "sensitive": True, "base_life": 3},
    {"name": "Çorba", "category": "Pişmiş Yemek", "sensitive": True, "base_life": 3},
    {"name": "Zeytinyağlı Yemek", "category": "Pişmiş Yemek", "sensitive": True, "base_life": 4},
    {"name": "Etli Yemek", "category": "Pişmiş Yemek", "sensitive": True, "base_life": 3},

    # İçecek / Konserve / Kahvaltılık
    {"name": "Meyve Suyu", "category": "İçecek", "sensitive": True, "base_life": 7},
    {"name": "Açılmış Konserve", "category": "Konserve", "sensitive": True, "base_life": 4},
    {"name": "Kapalı Konserve", "category": "Konserve", "sensitive": False, "base_life": 365},
    {"name": "Reçel", "category": "Kahvaltılık", "sensitive": False, "base_life": 90},
    {"name": "Zeytin", "category": "Kahvaltılık", "sensitive": False, "base_life": 60},
]

storage_conditions = ["Buzdolabı", "Oda Sıcaklığı", "Dondurucu"]
seasons = ["İlkbahar", "Yaz", "Sonbahar", "Kış"]


def calculate_risk_score(
    days_until_expiration,
    days_since_opened,
    is_opened,
    is_sensitive,
    storage_condition,
    previous_waste_count,
    previous_shared_count,
    season,
):
    score = 0

    # Son kullanma tarihine kalan gün azaldıkça risk artar.
    if days_until_expiration <= 0:
        score += 45
    elif days_until_expiration <= 2:
        score += 35
    elif days_until_expiration <= 5:
        score += 25
    elif days_until_expiration <= 10:
        score += 12
    else:
        score += 4

    # Açılmış ürünlerde geçen gün sayısı riski artırır.
    if is_opened:
        score += 12

        if days_since_opened >= 5:
            score += 25
        elif days_since_opened >= 3:
            score += 16
        elif days_since_opened >= 1:
            score += 8

    # Hassas ürünlerde bozulma riski daha yüksektir.
    if is_sensitive:
        score += 15

    # Saklama koşulu riski etkiler.
    if storage_condition == "Oda Sıcaklığı" and is_sensitive:
        score += 18
    elif storage_condition == "Dondurucu":
        score -= 12
    elif storage_condition == "Buzdolabı":
        score -= 4

    # Yaz aylarında hassas ürünlerde risk artabilir.
    if season == "Yaz" and is_sensitive:
        score += 8

    # Kullanıcının geçmiş israf davranışı riski artırır.
    score += previous_waste_count * 4

    # Daha önce paylaşım yapan kullanıcıda risk biraz azaltılır.
    score -= previous_shared_count * 2

    return max(0, min(100, score))

def get_risk_label(score):
    if score >= 80:
        return "Critical"

    if score >= 60:
        return "High"

    if score >= 30:
        return "Medium"

    return "Low"

def generate_single_row():
    product = random.choice(products)

    product_name = product["name"]
    category = product["category"]
    is_sensitive = product["sensitive"]
    base_life = product["base_life"]

    storage_condition = random.choice(storage_conditions)
    season = random.choice(seasons)

    days_until_expiration = random.randint(-3, base_life)
    is_opened = random.choice([True, False])

    if is_opened:
        days_since_opened = random.randint(1, 10)
    else:
        days_since_opened = 0

    quantity = round(random.uniform(0.25, 5.0), 2)
    previous_waste_count = random.randint(0, 6)
    previous_shared_count = random.randint(0, 5)

    risk_score = calculate_risk_score(
        days_until_expiration=days_until_expiration,
        days_since_opened=days_since_opened,
        is_opened=is_opened,
        is_sensitive=is_sensitive,
        storage_condition=storage_condition,
        previous_waste_count=previous_waste_count,
        previous_shared_count=previous_shared_count,
        season=season,
    )

    risk_label = get_risk_label(risk_score)

    return {
        "ProductName": product_name,
        "Category": category,
        "StorageCondition": storage_condition,
        "DaysUntilExpiration": days_until_expiration,
        "DaysSinceOpened": days_since_opened,
        "IsOpened": is_opened,
        "IsSensitive": is_sensitive,
        "Quantity": quantity,
        "PreviousWasteCount": previous_waste_count,
        "PreviousSharedCount": previous_shared_count,
        "Season": season,
        "RiskScore": risk_score,
        "RiskLabel": risk_label,
    }


def generate_dataset(row_count=3000):
    rows = []

    target_per_class = row_count // 4

    class_counts = {
        "Low": 0,
        "Medium": 0,
        "High": 0,
        "Critical": 0,
    }

    max_attempt_count = row_count * 50
    attempt_count = 0

    while min(class_counts.values()) < target_per_class and attempt_count < max_attempt_count:
        row = generate_single_row()
        risk_label = row["RiskLabel"]

        if class_counts[risk_label] < target_per_class:
            rows.append(row)
            class_counts[risk_label] += 1

        attempt_count += 1

    # Eğer bazı sınıflar hedefe tam ulaşamazsa kalan satırlar normal şekilde tamamlanır.
    while len(rows) < row_count:
        rows.append(generate_single_row())

    random.shuffle(rows)

    print("Sınıf dağılımı hedefi:")
    print(class_counts)

    return pd.DataFrame(rows)

dataset = generate_dataset(3000)

dataset.to_csv("food_waste_risk_dataset.csv", index=False, encoding="utf-8-sig")

print("Dataset oluşturuldu: food_waste_risk_dataset.csv")
print()
print("İlk 10 satır:")
print(dataset.head(10))
print()
print("Risk sınıfı dağılımı:")
print(dataset["RiskLabel"].value_counts())
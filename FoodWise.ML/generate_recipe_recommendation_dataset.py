# -*- coding: utf-8 -*-

# FoodWise için sentetik tarif öneri veri seti üretir.
# Bu veri seti, kullanıcının stok durumu ve tarif etkileşimlerine göre
# tariflere 0-100 arası öneri skoru vermek için kullanılacaktır.

import random
import pandas as pd


recipes = [
    # Kahvaltı
    {"name": "Sebzeli Omlet", "category": "Kahvaltı", "difficulty": "Kolay", "prep_time": 15},
    {"name": "Peynirli Tost", "category": "Kahvaltı", "difficulty": "Kolay", "prep_time": 10},
    {"name": "Yoğurtlu Yulaf", "category": "Kahvaltı", "difficulty": "Kolay", "prep_time": 8},
    {"name": "Menemen", "category": "Kahvaltı", "difficulty": "Kolay", "prep_time": 20},

    # Ana Yemek
    {"name": "Tavuklu Sebze Sote", "category": "Ana Yemek", "difficulty": "Orta", "prep_time": 35},
    {"name": "Kıymalı Makarna", "category": "Ana Yemek", "difficulty": "Orta", "prep_time": 30},
    {"name": "Sebzeli Pilav", "category": "Ana Yemek", "difficulty": "Kolay", "prep_time": 25},
    {"name": "Fırında Tavuk", "category": "Ana Yemek", "difficulty": "Orta", "prep_time": 45},
    {"name": "Etli Sebze Yemeği", "category": "Ana Yemek", "difficulty": "Zor", "prep_time": 55},

    # Çorba
    {"name": "Mercimek Çorbası", "category": "Çorba", "difficulty": "Kolay", "prep_time": 30},
    {"name": "Sebze Çorbası", "category": "Çorba", "difficulty": "Kolay", "prep_time": 28},
    {"name": "Yoğurt Çorbası", "category": "Çorba", "difficulty": "Orta", "prep_time": 35},
    {"name": "Domates Çorbası", "category": "Çorba", "difficulty": "Kolay", "prep_time": 25},

    # Salata
    {"name": "Mevsim Salata", "category": "Salata", "difficulty": "Kolay", "prep_time": 10},
    {"name": "Yoğurtlu Havuç Salatası", "category": "Salata", "difficulty": "Kolay", "prep_time": 15},
    {"name": "Tavuklu Salata", "category": "Salata", "difficulty": "Orta", "prep_time": 25},
    {"name": "Peynirli Akdeniz Salatası", "category": "Salata", "difficulty": "Kolay", "prep_time": 15},

    # Atıştırmalık
    {"name": "Bayat Ekmek Pizzası", "category": "Atıştırmalık", "difficulty": "Kolay", "prep_time": 20},
    {"name": "Muzlu Smoothie", "category": "Atıştırmalık", "difficulty": "Kolay", "prep_time": 7},
    {"name": "Sebzeli Sandviç", "category": "Atıştırmalık", "difficulty": "Kolay", "prep_time": 12},
    {"name": "Peynirli Lavaş Rulo", "category": "Atıştırmalık", "difficulty": "Kolay", "prep_time": 10},

    # Tatlı
    {"name": "Muzlu Pankek", "category": "Tatlı", "difficulty": "Orta", "prep_time": 25},
    {"name": "Elmalı Yulaf Tatlısı", "category": "Tatlı", "difficulty": "Kolay", "prep_time": 20},
    {"name": "Sütlaç", "category": "Tatlı", "difficulty": "Orta", "prep_time": 45},
    {"name": "Yoğurtlu Meyve Kasesi", "category": "Tatlı", "difficulty": "Kolay", "prep_time": 8},
]

seasons = ["İlkbahar", "Yaz", "Sonbahar", "Kış"]


def calculate_recommendation_score(
    matched_ingredient_count,
    missing_ingredient_count,
    matched_ingredient_ratio,
    risky_ingredient_count,
    average_days_until_expiration,
    has_sensitive_ingredient,
    user_liked_similar_recipes,
    user_saved_similar_recipes,
    user_cooked_similar_recipes,
    user_disliked_similar_recipes,
    viewed_similar_recipes,
    preparation_time_minutes,
    difficulty,
):
    score = 0

    # Tarifin stoktaki ürünlerle eşleşme oranı en önemli faktörlerden biridir.
    score += matched_ingredient_ratio * 35

    # Eşleşen malzeme sayısı arttıkça öneri güçlenir.
    score += matched_ingredient_count * 4

    # Eksik malzeme sayısı arttıkça öneri zayıflar.
    score -= missing_ingredient_count * 6

    # Riskli ürünleri değerlendiren tarifler daha çok önerilir.
    score += risky_ingredient_count * 9

    # Son kullanma tarihi yaklaşan ürünleri kullanan tarifler daha yüksek skor alır.
    if average_days_until_expiration <= 1:
        score += 18
    elif average_days_until_expiration <= 3:
        score += 14
    elif average_days_until_expiration <= 7:
        score += 8
    else:
        score += 2

    # Hassas ürün içeren tarifler, ürün riskliyse değerlendirme açısından önemlidir.
    if has_sensitive_ingredient:
        score += 6

    # Kullanıcının geçmiş olumlu davranışları skoru artırır.
    score += user_liked_similar_recipes * 4
    score += user_saved_similar_recipes * 5
    score += user_cooked_similar_recipes * 6

    # Kullanıcı benzer tarifleri beğenmediyse skor düşer.
    score -= user_disliked_similar_recipes * 8

    # Daha önce benzer tarifleri görüntülemiş olması küçük pozitif sinyal kabul edilir.
    score += min(viewed_similar_recipes, 10) * 1.2

    # Çok uzun hazırlık süresi kullanıcı için öneriyi zayıflatabilir.
    if preparation_time_minutes <= 15:
        score += 6
    elif preparation_time_minutes <= 30:
        score += 3
    elif preparation_time_minutes >= 50:
        score -= 6

    # Zor tarifler biraz daha düşük öneri alır.
    if difficulty == "Kolay":
        score += 5
    elif difficulty == "Zor":
        score -= 5

    return round(max(0, min(100, score)), 2)


def get_recommendation_label(score):
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"


def generate_single_row():
    recipe = random.choice(recipes)

    total_ingredient_count = random.randint(4, 10)
    matched_ingredient_count = random.randint(0, total_ingredient_count)
    missing_ingredient_count = total_ingredient_count - matched_ingredient_count

    matched_ingredient_ratio = matched_ingredient_count / total_ingredient_count

    risky_ingredient_count = random.randint(0, min(4, matched_ingredient_count))
    average_days_until_expiration = random.randint(0, 30)

    has_sensitive_ingredient = random.choice([True, False])

    user_liked_similar_recipes = random.randint(0, 8)
    user_saved_similar_recipes = random.randint(0, 6)
    user_cooked_similar_recipes = random.randint(0, 5)
    user_disliked_similar_recipes = random.randint(0, 5)
    viewed_similar_recipes = random.randint(0, 15)

    recommendation_score = calculate_recommendation_score(
        matched_ingredient_count=matched_ingredient_count,
        missing_ingredient_count=missing_ingredient_count,
        matched_ingredient_ratio=matched_ingredient_ratio,
        risky_ingredient_count=risky_ingredient_count,
        average_days_until_expiration=average_days_until_expiration,
        has_sensitive_ingredient=has_sensitive_ingredient,
        user_liked_similar_recipes=user_liked_similar_recipes,
        user_saved_similar_recipes=user_saved_similar_recipes,
        user_cooked_similar_recipes=user_cooked_similar_recipes,
        user_disliked_similar_recipes=user_disliked_similar_recipes,
        viewed_similar_recipes=viewed_similar_recipes,
        preparation_time_minutes=recipe["prep_time"],
        difficulty=recipe["difficulty"],
    )

    recommendation_label = get_recommendation_label(recommendation_score)

    return {
        "RecipeName": recipe["name"],
        "RecipeCategory": recipe["category"],
        "Difficulty": recipe["difficulty"],
        "PreparationTimeMinutes": recipe["prep_time"],
        "TotalIngredientCount": total_ingredient_count,
        "MatchedIngredientCount": matched_ingredient_count,
        "MissingIngredientCount": missing_ingredient_count,
        "MatchedIngredientRatio": round(matched_ingredient_ratio, 3),
        "RiskyIngredientCount": risky_ingredient_count,
        "AverageDaysUntilExpiration": average_days_until_expiration,
        "HasSensitiveIngredient": has_sensitive_ingredient,
        "UserLikedSimilarRecipes": user_liked_similar_recipes,
        "UserSavedSimilarRecipes": user_saved_similar_recipes,
        "UserCookedSimilarRecipes": user_cooked_similar_recipes,
        "UserDislikedSimilarRecipes": user_disliked_similar_recipes,
        "ViewedSimilarRecipes": viewed_similar_recipes,
        "Season": random.choice(seasons),
        "RecommendationScore": recommendation_score,
        "RecommendationLabel": recommendation_label,
    }


def generate_dataset(row_count=3000):
    rows = []

    for _ in range(row_count):
        rows.append(generate_single_row())

    return pd.DataFrame(rows)


dataset = generate_dataset(3000)

dataset.to_csv("recipe_recommendation_dataset.csv", index=False, encoding="utf-8-sig")

print("Dataset oluşturuldu: recipe_recommendation_dataset.csv")
print()
print("İlk 10 satır:")
print(dataset.head(10))
print()
print("Öneri sınıfı dağılımı:")
print(dataset["RecommendationLabel"].value_counts())
print()
print("Öneri skoru özeti:")
print(dataset["RecommendationScore"].describe())
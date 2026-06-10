# -*- coding: utf-8 -*-

# FoodWise için sentetik paylaşım talebi eşleştirme veri seti üretir.
# Bu veri seti, bir talep gönderen kullanıcının bir paylaşım ilanı için
# ne kadar uygun alıcı olduğunu 0-100 arası MatchScore ile tahmin etmek için kullanılacaktır.

import random
import pandas as pd


product_risk_levels = ["Low", "Medium", "High", "Critical"]
request_hours = list(range(8, 23))

product_categories = [
    "Süt Ürünleri",
    "Et Ürünleri",
    "Sebze",
    "Meyve",
    "Fırın Ürünü",
    "Kuru Gıda",
    "Pişmiş Yemek",
    "Konserve",
]


def calculate_match_score(
    same_city,
    same_district,
    same_neighborhood,
    distance_priority,
    need_score,
    reliability_score,
    completed_delivery_count,
    cancelled_request_count,
    pending_request_count,
    previous_successful_requests,
    product_risk_level,
    days_until_expiration,
    is_sensitive_food,
    donor_past_share_count,
    requester_past_receive_count,
    request_hour,
):
    score = 0

    # Konum yakınlığı en önemli faktörlerden biridir.
    if same_neighborhood:
        score += 28
    elif same_district:
        score += 22
    elif same_city:
        score += 14
    else:
        score += 4

    # Teslim noktasına yakınlık önceliği.
    # 1 = aynı mahalle, 2 = aynı ilçe, 3 = aynı şehir, 99 = uzak bölge
    if distance_priority == 1:
        score += 18
    elif distance_priority == 2:
        score += 13
    elif distance_priority == 3:
        score += 8
    else:
        score -= 8

    # Kullanıcının ihtiyaç skoru eşleşmeyi artırır.
    score += need_score * 0.22

    # Güvenilirlik skoru eşleşmeyi artırır.
    score += reliability_score * 0.26

    # Daha önce teslimat tamamlamış kullanıcı olumlu değerlendirilir.
    score += min(completed_delivery_count, 20) * 1.1

    # Daha önce başarılı talepleri olan kullanıcı olumlu değerlendirilir.
    score += min(previous_successful_requests, 15) * 1.3

    # İptal edilen talepler skoru düşürür.
    score -= cancelled_request_count * 4.5

    # Çok fazla bekleyen talep varsa öncelik biraz düşer.
    score -= pending_request_count * 2.2

    # Ürün riskliyse hızlı ve uygun kullanıcı daha değerli olabilir.
    if product_risk_level == "Critical":
        score += 12
    elif product_risk_level == "High":
        score += 9
    elif product_risk_level == "Medium":
        score += 5

    # Son kullanma tarihi çok yakınsa eşleşme daha önemli hale gelir.
    if days_until_expiration <= 0:
        score += 12
    elif days_until_expiration <= 1:
        score += 10
    elif days_until_expiration <= 3:
        score += 7
    elif days_until_expiration <= 7:
        score += 3

    # Hassas ürünlerde güvenilir alıcı daha önemli olduğu için reliability etkisi artırılır.
    if is_sensitive_food:
        score += 5

        if reliability_score >= 75:
            score += 6
        elif reliability_score < 45:
            score -= 8

    # Bağışçının geçmiş paylaşım sayısı, ilan güvenilirliği açısından küçük pozitif etki yapar.
    score += min(donor_past_share_count, 20) * 0.6

    # Alıcının geçmişte teslim aldığı ürün sayısı küçük pozitif sinyaldir.
    score += min(requester_past_receive_count, 20) * 0.8

    # Çok geç saatte gelen talepler küçük negatif etki alır.
    if request_hour >= 21:
        score -= 4
    elif 10 <= request_hour <= 18:
        score += 3

    return round(max(0, min(100, score)), 2)


def get_match_label(score):
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"


def generate_single_row():
    same_city = random.choices([True, False], weights=[85, 15])[0]

    if same_city:
        same_district = random.choices([True, False], weights=[65, 35])[0]
    else:
        same_district = False

    if same_district:
        same_neighborhood = random.choices([True, False], weights=[45, 55])[0]
    else:
        same_neighborhood = False

    if same_neighborhood:
        distance_priority = 1
    elif same_district:
        distance_priority = 2
    elif same_city:
        distance_priority = 3
    else:
        distance_priority = 99

    need_score = random.randint(0, 100)
    reliability_score = random.randint(20, 100)

    completed_delivery_count = random.randint(0, 30)
    cancelled_request_count = random.randint(0, 8)
    pending_request_count = random.randint(0, 6)
    previous_successful_requests = random.randint(0, 20)

    product_risk_level = random.choice(product_risk_levels)
    days_until_expiration = random.randint(-2, 20)
    is_sensitive_food = random.choice([True, False])

    donor_past_share_count = random.randint(0, 25)
    requester_past_receive_count = random.randint(0, 25)

    request_hour = random.choice(request_hours)
    product_category = random.choice(product_categories)

    match_score = calculate_match_score(
        same_city=same_city,
        same_district=same_district,
        same_neighborhood=same_neighborhood,
        distance_priority=distance_priority,
        need_score=need_score,
        reliability_score=reliability_score,
        completed_delivery_count=completed_delivery_count,
        cancelled_request_count=cancelled_request_count,
        pending_request_count=pending_request_count,
        previous_successful_requests=previous_successful_requests,
        product_risk_level=product_risk_level,
        days_until_expiration=days_until_expiration,
        is_sensitive_food=is_sensitive_food,
        donor_past_share_count=donor_past_share_count,
        requester_past_receive_count=requester_past_receive_count,
        request_hour=request_hour,
    )

    match_label = get_match_label(match_score)

    return {
        "SameCity": same_city,
        "SameDistrict": same_district,
        "SameNeighborhood": same_neighborhood,
        "DistancePriority": distance_priority,
        "NeedScore": need_score,
        "ReliabilityScore": reliability_score,
        "CompletedDeliveryCount": completed_delivery_count,
        "CancelledRequestCount": cancelled_request_count,
        "PendingRequestCount": pending_request_count,
        "PreviousSuccessfulRequests": previous_successful_requests,
        "ProductRiskLevel": product_risk_level,
        "DaysUntilExpiration": days_until_expiration,
        "IsSensitiveFood": is_sensitive_food,
        "DonorPastShareCount": donor_past_share_count,
        "RequesterPastReceiveCount": requester_past_receive_count,
        "RequestHour": request_hour,
        "ProductCategory": product_category,
        "MatchScore": match_score,
        "MatchLabel": match_label,
    }


def generate_dataset(row_count=3000):
    rows = []

    for _ in range(row_count):
        rows.append(generate_single_row())

    return pd.DataFrame(rows)


dataset = generate_dataset(3000)

dataset.to_csv("share_matching_dataset.csv", index=False, encoding="utf-8-sig")

print("Dataset oluşturuldu: share_matching_dataset.csv")
print()
print("İlk 10 satır:")
print(dataset.head(10))
print()
print("Eşleşme sınıfı dağılımı:")
print(dataset["MatchLabel"].value_counts())
print()
print("Eşleşme skoru özeti:")
print(dataset["MatchScore"].describe())
# -*- coding: utf-8 -*-

# FoodWise akıllı paylaşım eşleştirme modeli eğitimi.
# Bu dosya share_matching_dataset.csv veri setini kullanarak
# paylaşım talebi için 0-100 arası MatchScore tahmini yapan model eğitir.

import json
import joblib
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt

from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder


DATASET_PATH = "share_matching_dataset.csv"

MODEL_OUTPUT_PATH = "foodwise_share_matching_model.pkl"
METRICS_OUTPUT_PATH = "share_matching_model_metrics.txt"
FEATURE_IMPORTANCE_PATH = "share_matching_feature_importance_top20.csv"
PREDICTION_PLOT_PATH = "share_matching_prediction_scatter.png"
MODEL_INFO_PATH = "share_matching_model_info.json"


def load_dataset():
    dataset = pd.read_csv(DATASET_PATH, encoding="utf-8-sig")

    print("Paylaşım eşleştirme dataset yüklendi.")
    print(f"Satır sayısı: {len(dataset)}")
    print(f"Sütunlar: {list(dataset.columns)}")
    print()

    return dataset


def prepare_features(dataset):
    # MatchLabel, MatchScore üzerinden türetilmiş bir etikettir.
    # Bu yüzden modele girdi olarak verilmez.
    # Modelin hedef değişkeni 0-100 arası MatchScore değeridir.

    feature_columns = [
        "SameCity",
        "SameDistrict",
        "SameNeighborhood",
        "DistancePriority",
        "NeedScore",
        "ReliabilityScore",
        "CompletedDeliveryCount",
        "CancelledRequestCount",
        "PendingRequestCount",
        "PreviousSuccessfulRequests",
        "ProductRiskLevel",
        "DaysUntilExpiration",
        "IsSensitiveFood",
        "DonorPastShareCount",
        "RequesterPastReceiveCount",
        "RequestHour",
        "ProductCategory",
    ]

    target_column = "MatchScore"

    X = dataset[feature_columns].copy()
    y = dataset[target_column]

    categorical_features = [
        "ProductRiskLevel",
        "ProductCategory",
    ]

    numeric_features = [
        "SameCity",
        "SameDistrict",
        "SameNeighborhood",
        "DistancePriority",
        "NeedScore",
        "ReliabilityScore",
        "CompletedDeliveryCount",
        "CancelledRequestCount",
        "PendingRequestCount",
        "PreviousSuccessfulRequests",
        "DaysUntilExpiration",
        "IsSensitiveFood",
        "DonorPastShareCount",
        "RequesterPastReceiveCount",
        "RequestHour",
    ]

    # Boolean alanları 0/1 formatına çeviriyoruz.
    X["SameCity"] = X["SameCity"].astype(int)
    X["SameDistrict"] = X["SameDistrict"].astype(int)
    X["SameNeighborhood"] = X["SameNeighborhood"].astype(int)
    X["IsSensitiveFood"] = X["IsSensitiveFood"].astype(int)

    return X, y, categorical_features, numeric_features


def build_model(categorical_features, numeric_features):
    preprocessor = ColumnTransformer(
        transformers=[
            (
                "categorical",
                OneHotEncoder(handle_unknown="ignore"),
                categorical_features,
            ),
            (
                "numeric",
                "passthrough",
                numeric_features,
            ),
        ]
    )

    model = RandomForestRegressor(
        n_estimators=250,
        max_depth=14,
        random_state=42,
        min_samples_leaf=2,
    )

    pipeline = Pipeline(
        steps=[
            ("preprocessor", preprocessor),
            ("model", model),
        ]
    )

    return pipeline


def get_match_label(score):
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"


def save_prediction_plot(y_test, y_pred):
    plt.figure(figsize=(8, 6))
    plt.scatter(y_test, y_pred, alpha=0.55)
    plt.plot([0, 100], [0, 100], linestyle="--")
    plt.title("FoodWise Akıllı Eşleştirme Modeli - Gerçek vs Tahmin")
    plt.xlabel("Gerçek MatchScore")
    plt.ylabel("Tahmin Edilen MatchScore")
    plt.xlim(0, 100)
    plt.ylim(0, 100)
    plt.tight_layout()
    plt.savefig(PREDICTION_PLOT_PATH)
    plt.close()


def save_feature_importance(pipeline, categorical_features, numeric_features):
    preprocessor = pipeline.named_steps["preprocessor"]
    model = pipeline.named_steps["model"]

    categorical_names = (
        preprocessor
        .named_transformers_["categorical"]
        .get_feature_names_out(categorical_features)
        .tolist()
    )

    feature_names = categorical_names + numeric_features

    importance_df = pd.DataFrame(
        {
            "Feature": feature_names,
            "Importance": model.feature_importances_,
        }
    ).sort_values(by="Importance", ascending=False)

    importance_df.head(20).to_csv(
        FEATURE_IMPORTANCE_PATH,
        index=False,
        encoding="utf-8-sig",
    )


def save_model_info(dataset, mae, rmse, r2):
    info = {
        "model_name": "FoodWise Smart Share Matching Model",
        "algorithm": "RandomForestRegressor",
        "dataset_file": DATASET_PATH,
        "row_count": int(len(dataset)),
        "target_column": "MatchScore",
        "score_range": "0-100",
        "mae": round(float(mae), 4),
        "rmse": round(float(rmse), 4),
        "r2_score": round(float(r2), 4),
        "note": "Bu model sentetik FoodWise paylaşım eşleştirme veri seti ile eğitilmiştir."
    }

    with open(MODEL_INFO_PATH, "w", encoding="utf-8") as file:
        json.dump(info, file, ensure_ascii=False, indent=4)


def train_model():
    dataset = load_dataset()

    print("MatchLabel dağılımı:")
    print(dataset["MatchLabel"].value_counts())
    print()

    X, y, categorical_features, numeric_features = prepare_features(dataset)

    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=0.2,
        random_state=42,
    )

    pipeline = build_model(categorical_features, numeric_features)

    print("Akıllı eşleştirme modeli eğitiliyor...")
    pipeline.fit(X_train, y_train)

    y_pred = pipeline.predict(X_test)
    y_pred = np.clip(y_pred, 0, 100)

    mae = mean_absolute_error(y_test, y_pred)
    rmse = np.sqrt(mean_squared_error(y_test, y_pred))
    r2 = r2_score(y_test, y_pred)

    print()
    print("Akıllı eşleştirme modeli eğitimi tamamlandı.")
    print(f"MAE: {mae:.4f}")
    print(f"RMSE: {rmse:.4f}")
    print(f"R2 Score: {r2:.4f}")

    with open(METRICS_OUTPUT_PATH, "w", encoding="utf-8") as file:
        file.write("FoodWise Smart Share Matching Model Metrics\n")
        file.write("===========================================\n\n")
        file.write(f"MAE: {mae:.4f}\n")
        file.write(f"RMSE: {rmse:.4f}\n")
        file.write(f"R2 Score: {r2:.4f}\n\n")
        file.write("Açıklama:\n")
        file.write("MAE, tahmin edilen eşleşme skorunun gerçek skordan ortalama kaç puan saptığını gösterir.\n")
        file.write("RMSE, büyük hataları daha fazla cezalandıran hata metriğidir.\n")
        file.write("R2 Score, modelin hedef değişkendeki varyansı ne kadar açıkladığını gösterir.\n")

    save_prediction_plot(y_test, y_pred)
    save_feature_importance(pipeline, categorical_features, numeric_features)
    save_model_info(dataset, mae, rmse, r2)

    joblib.dump(pipeline, MODEL_OUTPUT_PATH)

    print()
    print("Oluşturulan dosyalar:")
    print(f"- {MODEL_OUTPUT_PATH}")
    print(f"- {METRICS_OUTPUT_PATH}")
    print(f"- {FEATURE_IMPORTANCE_PATH}")
    print(f"- {PREDICTION_PLOT_PATH}")
    print(f"- {MODEL_INFO_PATH}")

    print()
    print("Örnek tahmin:")

    sample = pd.DataFrame(
        [
            {
                "SameCity": 1,
                "SameDistrict": 1,
                "SameNeighborhood": 1,
                "DistancePriority": 1,
                "NeedScore": 85,
                "ReliabilityScore": 90,
                "CompletedDeliveryCount": 8,
                "CancelledRequestCount": 1,
                "PendingRequestCount": 1,
                "PreviousSuccessfulRequests": 6,
                "ProductRiskLevel": "High",
                "DaysUntilExpiration": 1,
                "IsSensitiveFood": 1,
                "DonorPastShareCount": 10,
                "RequesterPastReceiveCount": 7,
                "RequestHour": 14,
                "ProductCategory": "Süt Ürünleri",
            }
        ]
    )

    sample_score = pipeline.predict(sample)[0]
    sample_score = float(np.clip(sample_score, 0, 100))
    sample_label = get_match_label(sample_score)

    print(f"Tahmin edilen eşleşme skoru: {sample_score:.2f}")
    print(f"Tahmin edilen eşleşme seviyesi: {sample_label}")


if __name__ == "__main__":
    train_model()
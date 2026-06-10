# -*- coding: utf-8 -*-

# FoodWise tarif öneri modeli eğitimi.
# Bu dosya recipe_recommendation_dataset.csv veri setini kullanarak
# tariflere 0-100 arası RecommendationScore tahmini yapan model eğitir.

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


DATASET_PATH = "recipe_recommendation_dataset.csv"

MODEL_OUTPUT_PATH = "foodwise_recipe_recommendation_model.pkl"
METRICS_OUTPUT_PATH = "recipe_model_metrics.txt"
FEATURE_IMPORTANCE_PATH = "recipe_feature_importance_top20.csv"
PREDICTION_PLOT_PATH = "recipe_prediction_scatter.png"
MODEL_INFO_PATH = "recipe_model_info.json"


def load_dataset():
    dataset = pd.read_csv(DATASET_PATH, encoding="utf-8-sig")

    print("Tarif öneri dataset yüklendi.")
    print(f"Satır sayısı: {len(dataset)}")
    print(f"Sütunlar: {list(dataset.columns)}")
    print()

    return dataset


def prepare_features(dataset):
    # RecommendationLabel ve RecommendationScore aynı öneri mantığından üretildiği için
    # RecommendationLabel modele girdi olarak verilmez. Hedef değişken RecommendationScore'dur.

    feature_columns = [
        "RecipeName",
        "RecipeCategory",
        "Difficulty",
        "PreparationTimeMinutes",
        "TotalIngredientCount",
        "MatchedIngredientCount",
        "MissingIngredientCount",
        "MatchedIngredientRatio",
        "RiskyIngredientCount",
        "AverageDaysUntilExpiration",
        "HasSensitiveIngredient",
        "UserLikedSimilarRecipes",
        "UserSavedSimilarRecipes",
        "UserCookedSimilarRecipes",
        "UserDislikedSimilarRecipes",
        "ViewedSimilarRecipes",
        "Season",
    ]

    target_column = "RecommendationScore"

    X = dataset[feature_columns].copy()
    y = dataset[target_column]

    categorical_features = [
        "RecipeName",
        "RecipeCategory",
        "Difficulty",
        "Season",
    ]

    numeric_features = [
        "PreparationTimeMinutes",
        "TotalIngredientCount",
        "MatchedIngredientCount",
        "MissingIngredientCount",
        "MatchedIngredientRatio",
        "RiskyIngredientCount",
        "AverageDaysUntilExpiration",
        "HasSensitiveIngredient",
        "UserLikedSimilarRecipes",
        "UserSavedSimilarRecipes",
        "UserCookedSimilarRecipes",
        "UserDislikedSimilarRecipes",
        "ViewedSimilarRecipes",
    ]

    X["HasSensitiveIngredient"] = X["HasSensitiveIngredient"].astype(int)

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


def get_recommendation_label(score):
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"


def save_prediction_plot(y_test, y_pred):
    plt.figure(figsize=(8, 6))
    plt.scatter(y_test, y_pred, alpha=0.55)
    plt.plot([0, 100], [0, 100], linestyle="--")
    plt.title("FoodWise Tarif Öneri Modeli - Gerçek vs Tahmin")
    plt.xlabel("Gerçek RecommendationScore")
    plt.ylabel("Tahmin Edilen RecommendationScore")
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
        "model_name": "FoodWise Recipe Recommendation Score Model",
        "algorithm": "RandomForestRegressor",
        "dataset_file": DATASET_PATH,
        "row_count": int(len(dataset)),
        "target_column": "RecommendationScore",
        "score_range": "0-100",
        "mae": round(float(mae), 4),
        "rmse": round(float(rmse), 4),
        "r2_score": round(float(r2), 4),
        "note": "Bu model sentetik FoodWise tarif öneri veri seti ile eğitilmiştir."
    }

    with open(MODEL_INFO_PATH, "w", encoding="utf-8") as file:
        json.dump(info, file, ensure_ascii=False, indent=4)


def train_model():
    dataset = load_dataset()

    X, y, categorical_features, numeric_features = prepare_features(dataset)

    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=0.2,
        random_state=42,
    )

    pipeline = build_model(categorical_features, numeric_features)

    print("Tarif öneri modeli eğitiliyor...")
    pipeline.fit(X_train, y_train)

    y_pred = pipeline.predict(X_test)
    y_pred = np.clip(y_pred, 0, 100)

    mae = mean_absolute_error(y_test, y_pred)
    rmse = np.sqrt(mean_squared_error(y_test, y_pred))
    r2 = r2_score(y_test, y_pred)

    print()
    print("Tarif öneri modeli eğitimi tamamlandı.")
    print(f"MAE: {mae:.4f}")
    print(f"RMSE: {rmse:.4f}")
    print(f"R2 Score: {r2:.4f}")

    with open(METRICS_OUTPUT_PATH, "w", encoding="utf-8") as file:
        file.write("FoodWise Recipe Recommendation Model Metrics\n")
        file.write("============================================\n\n")
        file.write(f"MAE: {mae:.4f}\n")
        file.write(f"RMSE: {rmse:.4f}\n")
        file.write(f"R2 Score: {r2:.4f}\n\n")
        file.write("Açıklama:\n")
        file.write("MAE, tahmin edilen skorun gerçek skordan ortalama kaç puan saptığını gösterir.\n")
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
                "RecipeName": "Sebzeli Omlet",
                "RecipeCategory": "Kahvaltı",
                "Difficulty": "Kolay",
                "PreparationTimeMinutes": 15,
                "TotalIngredientCount": 8,
                "MatchedIngredientCount": 6,
                "MissingIngredientCount": 2,
                "MatchedIngredientRatio": 0.75,
                "RiskyIngredientCount": 2,
                "AverageDaysUntilExpiration": 2,
                "HasSensitiveIngredient": 1,
                "UserLikedSimilarRecipes": 3,
                "UserSavedSimilarRecipes": 2,
                "UserCookedSimilarRecipes": 1,
                "UserDislikedSimilarRecipes": 0,
                "ViewedSimilarRecipes": 5,
                "Season": "Yaz",
            }
        ]
    )

    sample_score = pipeline.predict(sample)[0]
    sample_score = float(np.clip(sample_score, 0, 100))
    sample_label = get_recommendation_label(sample_score)

    print(f"Tahmin edilen öneri skoru: {sample_score:.2f}")
    print(f"Tahmin edilen öneri seviyesi: {sample_label}")


if __name__ == "__main__":
    train_model()
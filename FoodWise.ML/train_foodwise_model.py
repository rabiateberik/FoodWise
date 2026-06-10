# FoodWise gıda israf riski tahmin modeli eğitimi.
# Bu dosya food_waste_risk_dataset.csv veri setini kullanarak
# ürünlerin Low / Medium / High / Critical risk seviyesini tahmin eden model eğitir.

import json
import joblib
import pandas as pd
import matplotlib.pyplot as plt

from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder


DATASET_PATH = "food_waste_risk_dataset.csv"
MODEL_OUTPUT_PATH = "foodwise_risk_model.pkl"
METRICS_OUTPUT_PATH = "model_metrics.txt"
CONFUSION_MATRIX_PATH = "confusion_matrix.png"
FEATURE_IMPORTANCE_PATH = "feature_importance_top20.csv"
MODEL_INFO_PATH = "model_info.json"


def load_dataset():
    dataset = pd.read_csv(DATASET_PATH, encoding="utf-8-sig")

    print("Dataset yüklendi.")
    print(f"Satır sayısı: {len(dataset)}")
    print(f"Sütunlar: {list(dataset.columns)}")
    print()

    return dataset


def prepare_features(dataset):
    # RiskScore model girdisi olarak kullanılmıyor.
    # Çünkü RiskLabel zaten RiskScore üzerinden oluşturulduğu için bunu modele verirsek veri sızıntısı olur.
    feature_columns = [
        "ProductName",
        "Category",
        "StorageCondition",
        "DaysUntilExpiration",
        "DaysSinceOpened",
        "IsOpened",
        "IsSensitive",
        "Quantity",
        "PreviousWasteCount",
        "PreviousSharedCount",
        "Season",
    ]

    target_column = "RiskLabel"

    X = dataset[feature_columns]
    y = dataset[target_column]

    categorical_features = [
        "ProductName",
        "Category",
        "StorageCondition",
        "Season",
    ]

    numeric_features = [
        "DaysUntilExpiration",
        "DaysSinceOpened",
        "IsOpened",
        "IsSensitive",
        "Quantity",
        "PreviousWasteCount",
        "PreviousSharedCount",
    ]

    # Boolean alanları 0/1 formatına çeviriyoruz.
    X = X.copy()
    X["IsOpened"] = X["IsOpened"].astype(int)
    X["IsSensitive"] = X["IsSensitive"].astype(int)

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

    model = RandomForestClassifier(
        n_estimators=200,
        max_depth=12,
        random_state=42,
        class_weight="balanced",
    )

    pipeline = Pipeline(
        steps=[
            ("preprocessor", preprocessor),
            ("model", model),
        ]
    )

    return pipeline


def save_confusion_matrix(y_test, y_pred, labels):
    matrix = confusion_matrix(y_test, y_pred, labels=labels)

    plt.figure(figsize=(8, 6))
    plt.imshow(matrix)
    plt.title("FoodWise Risk Modeli Confusion Matrix")
    plt.xlabel("Tahmin Edilen")
    plt.ylabel("Gerçek")
    plt.xticks(range(len(labels)), labels, rotation=45)
    plt.yticks(range(len(labels)), labels)

    for i in range(len(labels)):
        for j in range(len(labels)):
            plt.text(j, i, matrix[i, j], ha="center", va="center")

    plt.tight_layout()
    plt.savefig(CONFUSION_MATRIX_PATH)
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


def save_model_info(dataset, accuracy, labels):
    info = {
        "model_name": "FoodWise Waste Risk Prediction Model",
        "algorithm": "RandomForestClassifier",
        "dataset_file": DATASET_PATH,
        "row_count": int(len(dataset)),
        "target_column": "RiskLabel",
        "labels": labels,
        "accuracy": round(float(accuracy), 4),
        "note": "Bu model sentetik FoodWise veri seti ile eğitilmiştir.",
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
        stratify=y,
    )

    pipeline = build_model(categorical_features, numeric_features)

    print("Model eğitiliyor...")
    pipeline.fit(X_train, y_train)

    y_pred = pipeline.predict(X_test)

    accuracy = accuracy_score(y_test, y_pred)
    report = classification_report(y_test, y_pred)

    print()
    print("Model eğitimi tamamlandı.")
    print(f"Accuracy: {accuracy:.4f}")
    print()
    print("Classification Report:")
    print(report)

    labels = ["Low", "Medium", "High", "Critical"]

    with open(METRICS_OUTPUT_PATH, "w", encoding="utf-8") as file:
        file.write("FoodWise Risk Prediction Model Metrics\n")
        file.write("=====================================\n\n")
        file.write(f"Accuracy: {accuracy:.4f}\n\n")
        file.write("Classification Report:\n")
        file.write(report)

    save_confusion_matrix(y_test, y_pred, labels)
    save_feature_importance(pipeline, categorical_features, numeric_features)
    save_model_info(dataset, accuracy, labels)

    joblib.dump(pipeline, MODEL_OUTPUT_PATH)

    print()
    print("Oluşturulan dosyalar:")
    print(f"- {MODEL_OUTPUT_PATH}")
    print(f"- {METRICS_OUTPUT_PATH}")
    print(f"- {CONFUSION_MATRIX_PATH}")
    print(f"- {FEATURE_IMPORTANCE_PATH}")
    print(f"- {MODEL_INFO_PATH}")

    print()
    print("Örnek tahmin:")

    sample = pd.DataFrame(
        [
            {
                "ProductName": "Yoğurt",
                "Category": "Süt Ürünleri",
                "StorageCondition": "Buzdolabı",
                "DaysUntilExpiration": 1,
                "DaysSinceOpened": 3,
                "IsOpened": 1,
                "IsSensitive": 1,
                "Quantity": 1.5,
                "PreviousWasteCount": 2,
                "PreviousSharedCount": 0,
                "Season": "Yaz",
            }
        ]
    )

    prediction = pipeline.predict(sample)[0]
    probabilities = pipeline.predict_proba(sample)[0]

    print(f"Tahmin edilen risk seviyesi: {prediction}")
    print("Sınıf olasılıkları:")

    for label, probability in zip(pipeline.classes_, probabilities):
        print(f"{label}: {probability:.4f}")


if __name__ == "__main__":
    train_model()
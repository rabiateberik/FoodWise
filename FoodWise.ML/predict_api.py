# -*- coding: utf-8 -*-

# FoodWise ML tahmin API servisidir.
# Eğitilen risk, tarif öneri ve akıllı eşleştirme modellerini yükler.
# ASP.NET Core API bu servis üzerinden ML tahminleri alır.

from typing import Dict

import joblib
import numpy as np
import pandas as pd
from fastapi import FastAPI
from pydantic import BaseModel


RISK_MODEL_PATH = "foodwise_risk_model.pkl"
RECIPE_MODEL_PATH = "foodwise_recipe_recommendation_model.pkl"
SHARE_MATCHING_MODEL_PATH = "foodwise_share_matching_model.pkl"


app = FastAPI(
    title="FoodWise ML Prediction API",
    description="FoodWise için gıda israf riski, tarif öneri skoru ve akıllı eşleştirme tahmini yapan ML servisidir.",
    version="1.0.0",
)


risk_model = joblib.load(RISK_MODEL_PATH)
recipe_model = joblib.load(RECIPE_MODEL_PATH)
share_matching_model = joblib.load(SHARE_MATCHING_MODEL_PATH)


class RiskPredictionRequest(BaseModel):
    productName: str
    category: str
    storageCondition: str
    daysUntilExpiration: int
    daysSinceOpened: int
    isOpened: bool
    isSensitive: bool
    quantity: float
    previousWasteCount: int
    previousSharedCount: int
    season: str


class RiskPredictionResponse(BaseModel):
    riskLabel: str
    probabilities: Dict[str, float]


class RecipeScorePredictionRequest(BaseModel):
    recipeName: str
    recipeCategory: str
    difficulty: str
    preparationTimeMinutes: int
    totalIngredientCount: int
    matchedIngredientCount: int
    missingIngredientCount: int
    matchedIngredientRatio: float
    riskyIngredientCount: int
    averageDaysUntilExpiration: int
    hasSensitiveIngredient: bool
    userLikedSimilarRecipes: int
    userSavedSimilarRecipes: int
    userCookedSimilarRecipes: int
    userDislikedSimilarRecipes: int
    viewedSimilarRecipes: int
    season: str


class RecipeScorePredictionResponse(BaseModel):
    recommendationScore: float
    recommendationLabel: str


class ShareMatchPredictionRequest(BaseModel):
    sameCity: bool
    sameDistrict: bool
    sameNeighborhood: bool
    distancePriority: int
    needScore: int
    reliabilityScore: int
    completedDeliveryCount: int
    cancelledRequestCount: int
    pendingRequestCount: int
    previousSuccessfulRequests: int
    productRiskLevel: str
    daysUntilExpiration: int
    isSensitiveFood: bool
    donorPastShareCount: int
    requesterPastReceiveCount: int
    requestHour: int
    productCategory: str


class ShareMatchPredictionResponse(BaseModel):
    matchScore: float
    matchLabel: str


@app.get("/")
def index():
    return {
        "message": "FoodWise ML Prediction API çalışıyor.",
        "endpoints": [
            "/predict-risk",
            "/predict-recipe-score",
            "/predict-match-score"
        ],
        "docs": "/docs",
    }


@app.post("/predict-risk", response_model=RiskPredictionResponse)
def predict_risk(request: RiskPredictionRequest):
    input_data = pd.DataFrame(
        [
            {
                "ProductName": request.productName,
                "Category": request.category,
                "StorageCondition": request.storageCondition,
                "DaysUntilExpiration": request.daysUntilExpiration,
                "DaysSinceOpened": request.daysSinceOpened,
                "IsOpened": int(request.isOpened),
                "IsSensitive": int(request.isSensitive),
                "Quantity": request.quantity,
                "PreviousWasteCount": request.previousWasteCount,
                "PreviousSharedCount": request.previousSharedCount,
                "Season": request.season,
            }
        ]
    )

    prediction = risk_model.predict(input_data)[0]
    probability_values = risk_model.predict_proba(input_data)[0]

    probabilities = {
        label: round(float(probability), 4)
        for label, probability in zip(risk_model.classes_, probability_values)
    }

    return RiskPredictionResponse(
        riskLabel=prediction,
        probabilities=probabilities,
    )


@app.post("/predict-recipe-score", response_model=RecipeScorePredictionResponse)
def predict_recipe_score(request: RecipeScorePredictionRequest):
    input_data = pd.DataFrame(
        [
            {
                "RecipeName": request.recipeName,
                "RecipeCategory": request.recipeCategory,
                "Difficulty": request.difficulty,
                "PreparationTimeMinutes": request.preparationTimeMinutes,
                "TotalIngredientCount": request.totalIngredientCount,
                "MatchedIngredientCount": request.matchedIngredientCount,
                "MissingIngredientCount": request.missingIngredientCount,
                "MatchedIngredientRatio": request.matchedIngredientRatio,
                "RiskyIngredientCount": request.riskyIngredientCount,
                "AverageDaysUntilExpiration": request.averageDaysUntilExpiration,
                "HasSensitiveIngredient": int(request.hasSensitiveIngredient),
                "UserLikedSimilarRecipes": request.userLikedSimilarRecipes,
                "UserSavedSimilarRecipes": request.userSavedSimilarRecipes,
                "UserCookedSimilarRecipes": request.userCookedSimilarRecipes,
                "UserDislikedSimilarRecipes": request.userDislikedSimilarRecipes,
                "ViewedSimilarRecipes": request.viewedSimilarRecipes,
                "Season": request.season,
            }
        ]
    )

    predicted_score = recipe_model.predict(input_data)[0]
    predicted_score = float(np.clip(predicted_score, 0, 100))
    predicted_score = round(predicted_score, 2)

    recommendation_label = get_recommendation_label(predicted_score)

    return RecipeScorePredictionResponse(
        recommendationScore=predicted_score,
        recommendationLabel=recommendation_label,
    )


@app.post("/predict-match-score", response_model=ShareMatchPredictionResponse)
def predict_match_score(request: ShareMatchPredictionRequest):
    input_data = pd.DataFrame(
        [
            {
                "SameCity": int(request.sameCity),
                "SameDistrict": int(request.sameDistrict),
                "SameNeighborhood": int(request.sameNeighborhood),
                "DistancePriority": request.distancePriority,
                "NeedScore": request.needScore,
                "ReliabilityScore": request.reliabilityScore,
                "CompletedDeliveryCount": request.completedDeliveryCount,
                "CancelledRequestCount": request.cancelledRequestCount,
                "PendingRequestCount": request.pendingRequestCount,
                "PreviousSuccessfulRequests": request.previousSuccessfulRequests,
                "ProductRiskLevel": request.productRiskLevel,
                "DaysUntilExpiration": request.daysUntilExpiration,
                "IsSensitiveFood": int(request.isSensitiveFood),
                "DonorPastShareCount": request.donorPastShareCount,
                "RequesterPastReceiveCount": request.requesterPastReceiveCount,
                "RequestHour": request.requestHour,
                "ProductCategory": request.productCategory,
            }
        ]
    )

    predicted_score = share_matching_model.predict(input_data)[0]
    predicted_score = float(np.clip(predicted_score, 0, 100))
    predicted_score = round(predicted_score, 2)

    match_label = get_match_label(predicted_score)

    return ShareMatchPredictionResponse(
        matchScore=predicted_score,
        matchLabel=match_label,
    )


def get_recommendation_label(score: float) -> str:
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"


def get_match_label(score: float) -> str:
    if score >= 75:
        return "High"

    if score >= 45:
        return "Medium"

    return "Low"
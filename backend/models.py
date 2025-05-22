from sqlalchemy import Column, Integer, String, DateTime
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.sql import func

Base = declarative_base()

class AIFeedback(Base):
    __tablename__ = "ai_feedback"
    id = Column(Integer, primary_key=True, index=True)
    music_id = Column(Integer, nullable=False)
    ai_tag = Column(String, nullable=False)
    user_feedback = Column(String, nullable=False) 
    user_id = Column(Integer, nullable=True) 
    created_at = Column(DateTime(timezone=True), server_default=func.now())
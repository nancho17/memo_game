extends Area2D
class_name CorrectaButton
@onready var collision_shape_2d: CollisionShape2D = $CollisionShape2D

func player_touch() -> void:
	collision_shape_2d.disabled = true
	visible = false
	queue_free()

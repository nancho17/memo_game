extends Node2D
@onready var camera_2d: Camera2D = $Camera2D

@onready var cursor_jugador: Sprite2D = $CursorJugador
@onready var cursor_jugador_area: Area2D = $CursorJugador/Area

@onready var correcta: CorrectaButton = %Correcta
@onready var spawn_de_correctos: Node2D = $SpawnDeCorrectos
const CORRECTA :PackedScene = preload("uid://d30tij26dlk8j")
const RADIO_JUEGO : float = 556.0

func _ready() -> void:
	correcta.area_entered.connect(entered)
	#add_one_correcta()

func add_one_correcta():
	var new_correcta = CORRECTA.instantiate()
	var angulo_pos = randf()*2
	new_correcta.position.x = RADIO_JUEGO
	new_correcta.transform=new_correcta.transform.rotated(angulo_pos*PI)
	spawn_de_correctos.add_child(new_correcta)

func entered(body: Node2D) -> void:
	print("entered ", body.name)

func _physics_process(delta: float) -> void:
	inputs()
	cursor_jugador.transform=cursor_jugador.transform.rotated(0.03*PI)


func inputs()->void:
	if Input.is_action_just_pressed("click_debug"):
		print("click ",get_global_mouse_position())
		#cursor_jugador.global_position=get_global_mouse_position()
	if Input.is_action_just_pressed("player_action"):
		var todas_areas = cursor_jugador_area.get_overlapping_areas()
		if todas_areas.is_empty():
			print("Fallo!")
		else:
			for area in todas_areas: 
				area.player_touch()
				print("ok!")
			add_one_correcta()

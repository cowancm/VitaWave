### notes on setting up env

please install anaconda or miniconda to make setting up python versioning and dependencies easier

### whenever you download one of them, you can make an environment by typing into cmd:
`conda create -n envName python=3.xx` where `envName` is the name of the environment you'd like to create, and `3.xx` is 
the version of python you'd like to set up for this environment 

### to use this env for the current context:
`conda activate envName`

### to install in this env:
activate the env, and then `pip install` or `conda install` will live only in this environment


### point cloud rendering

3.9 <= python <= 3.12 

imports
- open3d
- numpy
- keyboard
- pyserial

### making a conda env called sdp that holds these dependencies:
`conda create sdp python=3.12`
`conda activate sdp`
`pip install open3d numpy keyboard pyserial`

in whatever editor/context you are in, you will need to activate to be in the environment


